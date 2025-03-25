using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CyclicalFileWatcher.Base;
using CyclicalFileWatcher.Implementations;

namespace CyclicalFileWatcher;

public sealed class CyclicalFileWatcher<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    private readonly IFileStateManager<TFileStateContent> _fileStateManager;
    private readonly FileSubscriptionManager<TFileStateContent> _subscriptionManager;
    
    public CyclicalFileWatcher(IFileStateManagerConfiguration configuration)
    {
        _subscriptionManager = new FileSubscriptionManager<TFileStateContent>();
        _fileStateManager = new FileStateManager<TFileStateContent>(configuration, _subscriptionManager);
    }

    /// <exception cref="FileNotFoundException">file found on path filePath, ensure WatchAsync has been called</exception>
    public Task WatchAsync(FileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken)
    {
        return _fileStateManager.WatchAsync(parameters, cancellationToken);
    }
    
    /// <exception cref="FileNotFoundException">file found on path filePath, ensure WatchAsync has been called</exception>
    /// <exception cref="KeyNotFoundException">file was found, but key is not</exception>
    public Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken)
    {
        return _fileStateManager.GetAsync(filePath, fileKey, cancellationToken);
    }

    /// <exception cref="FileNotFoundException">file found on path filePath, ensure WatchAsync has been called</exception>
    public Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken)
    {
        return _fileStateManager.GetLatestAsync(filePath, cancellationToken);
    }

    public Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken)
    {
        return _subscriptionManager.SubscribeAsync(filePath, actionOnUpdate, cancellationToken);
    }

    public Task Unsubscribe(FileSubscription subscription, CancellationToken cancellationToken)
    {
        return _subscriptionManager.Unsubscribe(subscription, cancellationToken);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _fileStateManager.DisposeAsync();
        await _subscriptionManager.DisposeAsync();
    }
}