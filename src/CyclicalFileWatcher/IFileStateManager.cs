using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcher;

/// <summary>
/// Defines the contract for managing and retrieving file states, as well as monitoring file changes.
/// </summary>
/// <typeparam name="TFileStateContent">
/// Represents the type of the file state content, which implements <see cref="IFileStateContent"/>.
/// </typeparam>
public interface IFileStateManager<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    /// <inheritdoc cref="IFileWatcher{T}.WatchAsync"/>>
    Task WatchAsync(IFileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken);

    /// <inheritdoc cref="IFileWatcher{T}.GetAsync"/>>
    Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken);

    /// <inheritdoc cref="IFileWatcher{T}.GetLatestAsync"/>>
    Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken);
}