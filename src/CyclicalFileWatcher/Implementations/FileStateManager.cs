using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CyclicalFileWatcher.Base;
using Nito.AsyncEx;

namespace CyclicalFileWatcher.Implementations;

internal sealed class FileStateManager<TFileStateContent> : IFileStateManager<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly IFileStateManagerConfiguration _configuration;
    private readonly IFileSubscriptionTrigger<TFileStateContent> _trigger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _watchingTask;
    
    private readonly Dictionary<string, FileStateStorage<TFileStateContent>> _storagesByPath = new();
    private readonly ConcurrentDictionary<string, AsyncReaderWriterLock> _watchLocksByPath = new();

    public FileStateManager(IFileStateManagerConfiguration configuration, IFileSubscriptionTrigger<TFileStateContent> trigger)
    {
        _configuration = configuration;
        _trigger = trigger;
        _cancellationTokenSource = new CancellationTokenSource();
        _watchingTask = CreateWatchingTask(_cancellationTokenSource.Token);
    }
    
    /// <exception cref="FileNotFoundException">file found on path filePath, ensure WatchAsync has been called</exception>
    /// <exception cref="KeyNotFoundException">file was found, but key is not</exception>
    public async Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        using var _ = await AcquireReaderLockAsync(filePath, cts.Token);
        
        if (_storagesByPath.TryGetValue(filePath, out var storage))
            return await storage.GetFileStateAsync(fileKey, cts.Token);
        throw new FileNotFoundException($"No file found for path {filePath} and kid {fileKey}, existing keys are: {string.Join(',', _storagesByPath.Keys)}.");
    }
    
    /// <exception cref="FileNotFoundException">file found on path filePath, ensure WatchAsync has been called</exception>
    public async Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        using var _ = await AcquireReaderLockAsync(filePath, cts.Token);

        if (_storagesByPath.TryGetValue(filePath, out var storage))
            return await storage.GetLatestFileStateAsync(cts.Token);
        throw new FileNotFoundException($"No certificate found for path {filePath}, existing keys are: {string.Join(',', _storagesByPath.Keys)}.");
    }
    
    /// <exception cref="ArgumentException">depth must be greater than 0</exception>
    /// <exception cref="FileNotFoundException">file found on path filePath</exception>
    public async Task WatchAsync(FileWatcherParameters<TFileStateContent> fileWatcherParameters, CancellationToken cancellationToken)
    {
        using var _ = await AcquireWriterLockAsync(fileWatcherParameters.FilePath, cancellationToken);

        ValidateWatcherParameters(fileWatcherParameters);
        
        if (_storagesByPath.ContainsKey(fileWatcherParameters.FilePath))
            return;
        
        _storagesByPath[fileWatcherParameters.FilePath] = new FileStateStorage<TFileStateContent>(fileWatcherParameters);
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
                    try
                    {
                        var tasks = _storagesByPath.Select(async x =>
                        {
                            FileState<TFileStateContent> file;
                            using (var _ = await AcquireWriterLockAsync(x.Key, cancellationToken))
                            {
                                var hasReloadedFile = await x.Value.TryUpdateFileStateAsync(cancellationToken);
                                if (!hasReloadedFile)
                                    return;
                               
                                file = await x.Value.GetLatestFileStateAsync(cancellationToken);
                                await _configuration.ActionOnReloaded.Invoke();
                            }
                          
                            await _trigger.TriggerSubscriptionsAsync(file, cancellationToken);
                        });
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        if (e is AggregateException aggregateException)
                            e = aggregateException.Flatten();
                        await _configuration.ActionOnFailedReload.Invoke(e);
                    }
                    finally
                    {
                        await Task.Delay(_configuration.FileCheckInterval, cancellationToken);
                    }
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    
    private async Task<IDisposable> AcquireReaderLockAsync(string certificatePath, CancellationToken cancellationToken)
    {
        var watchLock = _watchLocksByPath.GetOrAdd(certificatePath, _ => new AsyncReaderWriterLock());
        return await watchLock.ReaderLockAsync(cancellationToken);
    }

    private async Task<IDisposable> AcquireWriterLockAsync(string certificatePath, CancellationToken cancellationToken)
    {
        var watchLock = _watchLocksByPath.GetOrAdd(certificatePath, _ => new AsyncReaderWriterLock());
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
        
        foreach (var storage in _storagesByPath.Values)
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