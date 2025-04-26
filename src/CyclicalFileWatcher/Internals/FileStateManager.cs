using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;
using Nito.AsyncEx;

namespace FileWatcher.Internals;

internal sealed class FileStateManager<TFileStateContent> : IFileStateManager<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly IFileStateManagerConfiguration _configuration;
    private readonly IFileSubscriptionTrigger<TFileStateContent> _trigger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _watchingTask;
    
    private readonly Dictionary<FileStateIdentifier, FileStateStorage<TFileStateContent>> _storages = new();
    private readonly ConcurrentDictionary<FileStateIdentifier, AsyncReaderWriterLock> _watchLocks = new();

    public FileStateManager(IFileStateManagerConfiguration configuration, IFileSubscriptionTrigger<TFileStateContent> trigger)
    {
        _configuration = configuration;
        _trigger = trigger;
        _cancellationTokenSource = new CancellationTokenSource();
        _watchingTask = CreateWatchingTask(_cancellationTokenSource.Token);
    }
    
    public async Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

        var identifier = new FileStateIdentifier(filePath);
        
        using var _ = await AcquireReaderLockAsync(identifier, cts.Token);
        
        if (_storages.TryGetValue(identifier, out var storage))
            return await storage.GetFileStateAsync(fileKey, cts.Token);
        throw new FileNotFoundException($"No file found for path {filePath} and kid {fileKey}, existing keys are: {string.Join(',', _storages.Keys)}.");
    }
    
    public async Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        var identifier = new FileStateIdentifier(filePath);
        
        using var _ = await AcquireReaderLockAsync(identifier, cts.Token);

        if (_storages.TryGetValue(identifier, out var storage))
            return await storage.GetLatestFileStateAsync(cts.Token);
        throw new FileNotFoundException($"No certificate found for path {filePath}, existing keys are: {string.Join(',', _storages.Keys)}.");
    }
    
    public async Task WatchAsync(FileWatcherParameters<TFileStateContent> fileWatcherParameters, CancellationToken cancellationToken)
    {
        var identifier = new FileStateIdentifier(fileWatcherParameters.FilePath);
        
        using var _ = await AcquireWriterLockAsync(identifier, cancellationToken);

        ValidateWatcherParameters(fileWatcherParameters);
        
        if (_storages.ContainsKey(identifier))
            return;
        
        _storages[identifier] = new FileStateStorage<TFileStateContent>(fileWatcherParameters);
    }

    private void ValidateWatcherParameters(FileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        if (fileWatcherParameters.Depth < 1)
            throw new ArgumentException("Depth must be greater than 0.");
        
        if (!File.Exists(fileWatcherParameters.FilePath))
            throw new FileNotFoundException($"File {fileWatcherParameters.FilePath} does not exist.");
    }
    
    private Task CreateWatchingTask(CancellationToken cancellationToken)
    {
        return Task.Factory.StartNew(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var tasks = _storages.Select(async x =>
                    {
                        using var _ = await AcquireWriterLockAsync(x.Key, cancellationToken);
                        
                        var file = await TryUpdateFileStateAsync(x.Key, x.Value, cancellationToken);
                        if (file == null)
                            return;
                       
                        await TryTriggerSubscriptionAsync(file, cancellationToken);
                    });

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (AggregateException e)
                    {
                        await ProcessFileUpdateExceptionAsync(e);
                        throw;
                    }
                    finally
                    {
                        await Task.Delay(_configuration.FileCheckInterval, cancellationToken);
                    }
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private async Task<FileState<TFileStateContent>?> TryUpdateFileStateAsync(FileStateIdentifier identifier, FileStateStorage<TFileStateContent> fileStateStorage, CancellationToken cancellationToken)
    {
        try
        {
            var hasReloadedFile = await fileStateStorage.TryUpdateFileStateAsync(cancellationToken);
            if (!hasReloadedFile)
                return null;
                            
            var file = await fileStateStorage.GetLatestFileStateAsync(cancellationToken);
            await _configuration.ActionOnFileReloaded.Invoke(file.Identifier);
            return file;
        }
        catch (Exception e)
        {
            throw new FileWatcherReloadException(identifier, e);
        }
    }

    private async Task TryTriggerSubscriptionAsync(FileState<TFileStateContent> file, CancellationToken cancellationToken)
    {
        try
        {
            await _trigger.TriggerSubscriptionsAsync(file, cancellationToken);
            await _configuration.ActionOnSubscribeAction.Invoke(file.Identifier);
        }
        catch (Exception e)
        {
            throw new FileWatcherSubscriptionException(file.Identifier, e);
        }
    }

    private async Task ProcessFileUpdateExceptionAsync(AggregateException e)
    {
        var exceptions = e.Flatten().InnerExceptions
            .ToList();
        foreach (var exception in exceptions)
        {
            if (exception is FileWatcherReloadException fileWatcherReloadException)
                await _configuration.ActionOnFileReloadFailed.Invoke(fileWatcherReloadException);
            else if (exception is FileWatcherSubscriptionException fileWatcherSubscriptionException)
                await _configuration.ActionOnSubscribeActionFailed.Invoke(fileWatcherSubscriptionException);
        }
    }
    
    private async Task<IDisposable> AcquireReaderLockAsync(FileStateIdentifier identifier, CancellationToken cancellationToken)
    {
        var watchLock = _watchLocks.GetOrAdd(identifier, _ => new AsyncReaderWriterLock());
        return await watchLock.ReaderLockAsync(cancellationToken);
    }

    private async Task<IDisposable> AcquireWriterLockAsync(FileStateIdentifier identifier, CancellationToken cancellationToken)
    {
        var watchLock = _watchLocks.GetOrAdd(identifier, _ => new AsyncReaderWriterLock());
        return await watchLock.WriterLockAsync(cancellationToken);
    }
    
    private async Task StopWatchingTaskAsync()
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;
        
        try
        {
            _cancellationTokenSource.Cancel();
            await _watchingTask; 
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
        {
            // Expected cancellation, suppress the exception.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopWatchingTaskAsync();
        await CastAndDispose(_cancellationTokenSource);
        await CastAndDispose(_watchingTask);
        
        foreach (var storage in _storages.Values)
            await storage.DisposeAsync();

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}