using System;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher;

public interface IFileSubscriptionManager<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    /// <inheritdoc cref="IFileWatcher{T}.SubscribeAsync"/>>
    Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken);

    /// <inheritdoc cref="IFileWatcher{T}.UnsubscribeAsync"/>>
    Task UnsubscribeAsync(FileSubscription subscription, CancellationToken cancellationToken);
}