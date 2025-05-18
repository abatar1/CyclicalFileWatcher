using System;
using System.Threading.Tasks;

namespace FileWatcher;

public sealed class FileStateManagerConfiguration : IFileStateManagerConfiguration
{
    public required TimeSpan FileCheckInterval { get; init; }
    
    public required Func<FileStateIdentifier, Task> ActionOnFileReloaded { get; init; }
    
    public required Func<FileWatcherReloadException, Task> ActionOnFileReloadFailed { get; init; }
    
    public required Func<FileWatcherSubscriptionException, Task> ActionOnSubscribeActionFailed { get; init; }
    
    public required Func<FileStateIdentifier, Task> ActionOnSubscribeAction { get; init; }
}