using System;

namespace FileWatcher;

/// <summary>
/// Represents an exception that occurs during the subscription process in the file watcher system.
/// </summary>
/// <remarks>
/// This exception is thrown when a subscription action encounters an error, such as
/// an issue with subscribing to updates for a specific file's state.
/// </remarks>
/// <param name="identifier">
/// The unique identifier for the file's state that caused the subscription failure.
/// </param>
/// <param name="innerException">
/// The exception that triggered this failure, providing additional details of the error.
/// </param>
public sealed class FileWatcherSubscriptionException(FileStateIdentifier identifier, Exception innerException)
    : Exception(innerException.Message, innerException)
{
    public FileStateIdentifier FileIdentifier { get; } = identifier;
}