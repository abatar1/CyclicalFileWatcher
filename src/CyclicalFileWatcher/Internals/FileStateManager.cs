using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher.Internals;

internal sealed class FileStateManager<TFileStateContent> : IFileStateManager<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly IFileStateStorageRepository<TFileStateContent> _fileStateStorageRepository;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ReadWriteLockProvider _lockProvider;
    private readonly IFileSystemProxy _fileSystemProxy;
    private readonly Task _watchingTask;

    public FileStateManager(
        IFileStateStorageRepository<TFileStateContent> fileStateStorageRepository,
        FileWatchProcessor<TFileStateContent> fileWatchProcessor, 
        ReadWriteLockProvider lockProvider, 
        IFileSystemProxy fileSystemProxy)
    {
        _fileStateStorageRepository = fileStateStorageRepository;
        _lockProvider = lockProvider;
        _fileSystemProxy = fileSystemProxy;
        _cancellationTokenSource = new CancellationTokenSource();
        _watchingTask = fileWatchProcessor.CreateWatchingTask(_cancellationTokenSource.Token);
    }
    
    public async Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

        var identifier = new FileStateIdentifier(filePath);
        
        using var _ = await _lockProvider.AcquireReaderLockAsync(identifier, cts.Token);
        
        if (_fileStateStorageRepository.TryGetValue(identifier, out var storage))
            return await storage.GetAsync(fileKey, cts.Token);
        throw new FileNotFoundException($"No file found for path {filePath} and kid {fileKey}).");
    }
    
    public async Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        var identifier = new FileStateIdentifier(filePath);
        
        using var _ = await _lockProvider.AcquireReaderLockAsync(identifier, cts.Token);

        if (_fileStateStorageRepository.TryGetValue(identifier, out var storage))
            return await storage.GetLatestAsync(cts.Token);
        throw new FileNotFoundException($"No certificate found for path {filePath}.");
    }
    
    public async Task WatchAsync(IFileWatcherParameters<TFileStateContent> fileWatcherParameters, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        var identifier = new FileStateIdentifier(fileWatcherParameters.FilePath);
        
        using var _ = await _lockProvider.AcquireWriterLockAsync(identifier, cts.Token);

        ValidateWatcherParameters(fileWatcherParameters);
        
        if (_fileStateStorageRepository.TryGetValue(identifier, out var _))
            return;
        
        _fileStateStorageRepository.Set(identifier, fileWatcherParameters);
    }

    private void ValidateWatcherParameters(IFileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        if (fileWatcherParameters.Depth < 1)
            throw new ArgumentException("Depth must be greater than 0.");
        
        if (!_fileSystemProxy.FileExists(fileWatcherParameters.FilePath))
            throw new FileNotFoundException($"File {fileWatcherParameters.FilePath} does not exist.");
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

        await _fileStateStorageRepository.DisposeAsync();

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