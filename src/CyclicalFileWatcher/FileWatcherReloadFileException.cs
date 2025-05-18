using System;

namespace FileWatcher;

/// <summary>
/// Represents an exception that is thrown when a file monitoring process fails to reload the state of a file.
/// </summary>
public sealed class FileWatcherReloadException(FileStateIdentifier identifier, Exception innerException)
    : Exception(innerException.Message, innerException)
{
    public FileStateIdentifier FileIdentifier { get; } = identifier;
}