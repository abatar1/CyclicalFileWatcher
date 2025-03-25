using System;
using System.Threading;
using System.Threading.Tasks;
using CyclicalFileWatcher.Base;

namespace CyclicalFileWatcher;

public interface IFileSubscriptionManager<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken);

    Task Unsubscribe(FileSubscription subscription, CancellationToken cancellationToken);
}