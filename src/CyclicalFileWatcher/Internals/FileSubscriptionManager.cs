using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher.Internals;

internal sealed class FileSubscriptionManager<TFileStateContent> : IFileSubscriptionManager<TFileStateContent>, IFileSubscriptionTrigger<TFileStateContent>, IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    private readonly Dictionary<FileStateIdentifier, Dictionary<Guid, Func<FileState<TFileStateContent>, Task>>> _subscriptions = new();
    private readonly ConcurrentDictionary<FileStateIdentifier, SemaphoreSlim> _subscribeLocks = new();

    public async Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnStateUpdate, CancellationToken cancellationToken)
    {
        var identifier = new FileStateIdentifier(filePath);
        var subscribeLock = _subscribeLocks.GetOrAdd(identifier, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);

        try
        {
            var subscriptionKey = Guid.NewGuid();
            if (_subscriptions.TryAdd(identifier, new Dictionary<Guid, Func<FileState<TFileStateContent>, Task>>()))
                _subscriptions[identifier].Add(subscriptionKey, actionOnStateUpdate);
            else
                _subscriptions[identifier].TryAdd(subscriptionKey, actionOnStateUpdate);

            return new FileSubscription
            {
                SubscriptionId = subscriptionKey,
                FilePath = filePath
            };
        }
        finally
        {
            subscribeLock.Release();
        }
    }

    public async Task Unsubscribe(FileSubscription subscription, CancellationToken cancellationToken)
    {
        var identifier = new FileStateIdentifier(subscription.FilePath);
        var subscribeLock = _subscribeLocks.GetOrAdd(identifier, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);
        
        try
        {
            if (_subscriptions.TryGetValue(identifier, out var subscriptions))
                subscriptions.Remove(subscription.SubscriptionId);
        }
        finally
        { 
            subscribeLock.Release();
        }
    }

    public async Task TriggerSubscriptionsAsync(FileState<TFileStateContent> fileState, CancellationToken cancellationToken)
    {
        var subscribeLock = _subscribeLocks.GetOrAdd(fileState.Identifier, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);
            
        try
        {
            if (_subscriptions.TryGetValue(fileState.Identifier, out var actionOnUpdate))
                await Task.WhenAll(actionOnUpdate.Keys.Select(x => actionOnUpdate[x].Invoke(fileState)));
        }
        finally
        {
            subscribeLock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var subscribeLock in _subscribeLocks.Values)
            await CastAndDispose(subscribeLock);
        
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