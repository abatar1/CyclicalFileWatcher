using System;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;
using FileWatcher.Internals;

namespace FileWatcher;

/// <summary>
/// Represents a cyclical file watcher that monitors file changes and allows subscribing
/// to file state updates. This class facilitates managing file states, retrieving the
/// latest file information, and providing mechanisms to handle file updates asynchronously.
/// </summary>
/// <typeparam name="TFileStateContent">
/// The type of content for file state management. It must implement the <see cref="IFileStateContent"/> interface.
/// </typeparam>
public sealed class CyclicalFileWatcher<TFileStateContent> : IFileWatcher<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly IFileStateManager<TFileStateContent> _fileStateManager;
    private readonly FileSubscriptionManager<TFileStateContent> _subscriptionManager;
    private readonly IFileStateStorageRepository<TFileStateContent> _repository;
    
    public CyclicalFileWatcher(IFileStateManagerConfiguration configuration)
    {
        _subscriptionManager = new FileSubscriptionManager<TFileStateContent>();
        var fileProxy = new FileSystemProxy();
        _repository = new FileStateStorageRepository<TFileStateContent>(fileProxy);
        var lockProvider = new ReadWriteLockProvider();
        var processor = new FileWatchProcessor<TFileStateContent>(configuration, _subscriptionManager, lockProvider, _repository);
        _fileStateManager = new FileStateManager<TFileStateContent>(_repository, processor, lockProvider, fileProxy);
    }
    
    internal CyclicalFileWatcher(IFileStateManagerConfiguration configuration, 
        IFileStateStorageRepository<TFileStateContent> repository, 
        IFileSystemProxy fileSystemProxy)
    {
        _repository = repository;
        _subscriptionManager = new FileSubscriptionManager<TFileStateContent>();
        var lockProvider = new ReadWriteLockProvider();
        var processor = new FileWatchProcessor<TFileStateContent>(configuration, _subscriptionManager, lockProvider, _repository);
        _fileStateManager = new FileStateManager<TFileStateContent>(_repository, processor, lockProvider, fileSystemProxy);
    }

    public Task WatchAsync(IFileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken)
    {
        return _fileStateManager.WatchAsync(parameters, cancellationToken);
    }
    
    public Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken)
    {
        return _fileStateManager.GetAsync(filePath, fileKey, cancellationToken);
    }
    
    public Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken)
    {
        return _fileStateManager.GetLatestAsync(filePath, cancellationToken);
    }

    public Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken)
    {
        return _subscriptionManager.SubscribeAsync(filePath, actionOnUpdate, cancellationToken);
    }

    public Task UnsubscribeAsync(FileSubscription subscription, CancellationToken cancellationToken)
    {
        return _subscriptionManager.UnsubscribeAsync(subscription, cancellationToken);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _fileStateManager.DisposeAsync();
        await _subscriptionManager.DisposeAsync();
        await _repository.DisposeAsync();
    }
}