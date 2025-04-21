using System;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher;

public interface IFileWatcher<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    Task WatchAsync(FileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken);

    Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken);

    Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken);

    Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken);

    Task UnsubscribeAsync(FileSubscription subscription, CancellationToken cancellationToken);
}