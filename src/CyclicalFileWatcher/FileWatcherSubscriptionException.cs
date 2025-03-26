using System;

namespace FileWatcher;

public sealed class FileWatcherSubscriptionException(FileStateIdentifier identifier, Exception innerException)
    : Exception(innerException.Message, innerException)
{
    public FileStateIdentifier FileIdentifier { get; } = identifier;
}