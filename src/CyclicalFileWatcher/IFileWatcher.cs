using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;

namespace FileWatcher;

/// <summary>
/// Represents a generic interface for watching file changes and managing file states asynchronously.
/// </summary>
/// <typeparam name="TFileStateContent">The type of content representing the state of the file being watched.</typeparam>
public interface IFileWatcher<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    /// <summary>
    /// Begins watching a file with the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters for file watching, including file path, depth, and factory functions for content and keys.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="ArgumentException">Thrown when the depth in parameters is less than or equal to 0.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file is not found on the given path.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when IFileWatcher has been disposed.</exception>
    Task WatchAsync(IFileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the state of a file using the specified file path and key.
    /// </summary>
    /// <param name="filePath">The file path used to locate the file.</param>
    /// <param name="fileKey">The key associated with the file's state to retrieve.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the state of the specified file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file is not found at the specified file path. Ensure WatchAsync has been called first.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the file is found but the specified key is not available.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when IFileWatcher has been disposed.</exception>
    Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the latest state of a file at the specified path.
    /// </summary>
    /// <param name="filePath">The path of the file to retrieve the latest state for.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The latest state of the specified file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found at the specified path. Ensure that <c>WatchAsync</c> has been called.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when IFileWatcher has been disposed.</exception>
    Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to updates for a specific file and executes an action whenever the file state changes.
    /// </summary>
    /// <param name="filePath">The path of the file to subscribe to for updates.</param>
    /// <param name="actionOnUpdate">The action to execute when the file state is updated. This function provides the updated file state.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, returning a <see cref="FileSubscription"/> instance containing subscription details.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when IFileWatcher has been disposed.</exception>
    Task<FileSubscription> SubscribeAsync(string filePath, Func<IFileState<TFileStateContent>, Task> actionOnUpdate, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a previously active subscription to file updates.
    /// </summary>
    /// <param name="subscription">The subscription that identifies the file and the callback to unsubscribe.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests during the unsubscribing process.</param>
    /// <returns>A task that represents the asynchronous operation of unsubscribing from the file updates.</returns>
    Task UnsubscribeAsync(FileSubscription subscription, CancellationToken cancellationToken);
}