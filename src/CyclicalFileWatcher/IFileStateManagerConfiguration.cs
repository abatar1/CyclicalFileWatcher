using System;
using System.Threading.Tasks;

namespace FileWatcher;

/// <summary>
/// Provides the configuration interface for managing file state operations in a file watcher system.
/// </summary>
public interface IFileStateManagerConfiguration
{
    TimeSpan FileCheckInterval { get; init; }
    
    Func<FileWatcherReloadException, Task> ActionOnFileReloadFailed { get; init; }
    
    Func<FileWatcherSubscriptionException, Task> ActionOnSubscribeActionFailed { get; init; }
    
    Func<FileStateIdentifier, Task> ActionOnFileReloaded { get; init; }
    
    Func<FileStateIdentifier, Task> ActionOnSubscribeAction { get; init; }
}