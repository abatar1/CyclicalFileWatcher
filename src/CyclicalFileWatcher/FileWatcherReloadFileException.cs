using System;

namespace FileWatcher;

public sealed class FileWatcherReloadException(FileStateIdentifier identifier, Exception innerException)
    : Exception(innerException.Message, innerException)
{
    public FileStateIdentifier FileIdentifier { get; } = identifier;
}