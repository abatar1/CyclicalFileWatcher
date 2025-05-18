using System;
using System.Threading.Tasks;

namespace FileWatcher;

/// <summary>
/// Provides the configuration interface for managing file state operations in a file watcher system.
/// </summary>
public interface IFileStateManagerConfiguration
{
    /// <summary>
    /// Specifies the interval at which the file watcher checks for changes to a file.
    /// </summary>
    /// <remarks>
    /// This property determines the frequency of file monitoring. A shorter interval results in
    /// more frequent checks, potentially increasing CPU usage. The value must be specified as a <see cref="TimeSpan"/>.
    /// </remarks>
    TimeSpan FileCheckInterval { get; init; }

    /// <summary>
    /// Defines the action to be executed when a file reload operation fails in the file watcher system.
    /// </summary>
    /// <remarks>
    /// This property specifies a delegate that accepts a <see cref="FileWatcherReloadException"/> instance and returns a <see cref="Task"/>.
    /// The provided action is invoked each time a file reload process encounters an unrecoverable error, allowing custom error handling logic to be implemented.
    /// </remarks>
    Func<FileWatcherReloadException, Task> ActionOnFileReloadFailed { get; init; }
    
    /// <summary>
    /// Defines the action to be executed when a file is successfully reloaded.
    /// </summary>
    /// <remarks>
    /// This property specifies a delegate that takes a <see cref="FileStateIdentifier"/> as input and performs
    /// an operation asynchronously when a file reload event is detected. It is invoked after the file state
    /// has been successfully reloaded and is primarily used to execute custom post-reload logic.
    /// </remarks>
    Func<FileWatcherSubscriptionException, Task> ActionOnSubscribeActionFailed { get; init; }
    
    /// <summary>
    /// Defines the action to be executed when a subscription action fails.
    /// </summary>
    /// <remarks>
    /// This property allows specifying a delegate that handles errors occurring during
    /// the subscription process. It receives a <see cref="FileWatcherSubscriptionException"/>,
    /// providing context about the failure, such as the associated file and the underlying exception.
    /// The action is executed asynchronously.
    /// </remarks>
    Func<FileStateIdentifier, Task> ActionOnFileReloaded { get; init; }
    
    /// <summary>
    /// Specifies the action to be executed when a subscription to a file is successfully established.
    /// </summary>
    /// <remarks>
    /// This property defines a delegate that accepts a <see cref="FileStateIdentifier"/> parameter, which identifies the subscribed file.
    /// The associated action is executed asynchronously and is typically used for custom operations upon successful subscription.
    /// </remarks>
    Func<FileStateIdentifier, Task> ActionOnSubscribeAction { get; init; }
}