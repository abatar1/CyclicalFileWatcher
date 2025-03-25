using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CyclicalFileWatcher.Base;

namespace CyclicalFileWatcher.Implementations;

internal sealed class FileSubscriptionManager<TFileStateContent> : IFileSubscriptionManager<TFileStateContent>, IFileSubscriptionTrigger<TFileStateContent>, IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    private readonly Dictionary<string, Dictionary<Guid, Func<FileState<TFileStateContent>, Task>>> _subscriptionsByPath = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _subscribeLocksByPath = new();

    public async Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnStateUpdate, CancellationToken cancellationToken)
    {
        var subscribeLock = _subscribeLocksByPath.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);

        try
        {
            var subscriptionKey = Guid.NewGuid();
            if (_subscriptionsByPath.TryAdd(filePath, new Dictionary<Guid, Func<FileState<TFileStateContent>, Task>>()))
                _subscriptionsByPath[filePath].Add(subscriptionKey, actionOnStateUpdate);
            else
                _subscriptionsByPath[filePath].TryAdd(subscriptionKey, actionOnStateUpdate);

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
        var subscribeLock = _subscribeLocksByPath.GetOrAdd(subscription.FilePath, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);
        
        try
        {
            if (_subscriptionsByPath.TryGetValue(subscription.FilePath, out var subscriptions))
                subscriptions.Remove(subscription.SubscriptionId);
        }
        finally
        { 
            subscribeLock.Release();
        }
    }

    public async Task TriggerSubscriptionsAsync(FileState<TFileStateContent> fileState, CancellationToken cancellationToken)
    {
        var subscribeLock = _subscribeLocksByPath.GetOrAdd(fileState.FilePath, _ => new SemaphoreSlim(1, 1));
        await subscribeLock.WaitAsync(cancellationToken);
            
        try
        {
            if (_subscriptionsByPath.TryGetValue(fileState.FilePath, out var actionOnUpdate))
                await Task.WhenAll(actionOnUpdate.Keys.Select(x => actionOnUpdate[x].Invoke(fileState)));
        }
        finally
        {
            subscribeLock.Release();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var subscribeLock in _subscribeLocksByPath.Values)
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