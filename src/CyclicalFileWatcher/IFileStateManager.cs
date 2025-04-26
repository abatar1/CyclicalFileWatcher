using System;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher;

public interface IFileStateManager<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    /// <inheritdoc cref="IFileWatcher{T}.WatchAsync"/>>
    Task WatchAsync(FileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken);

    /// <inheritdoc cref="IFileWatcher{T}.GetAsync"/>>
    Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken);

    /// <inheritdoc cref="IFileWatcher{T}.GetLatestAsync"/>>
    Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken);
}