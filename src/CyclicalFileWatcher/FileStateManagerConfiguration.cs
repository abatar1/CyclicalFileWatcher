using System;
using System.Threading.Tasks;

namespace CyclicalFileWatcher;

public sealed class FileStateManagerConfiguration : IFileStateManagerConfiguration
{
    public required TimeSpan FileCheckInterval { get; init; }
    
    public required Func<Exception, Task> ActionOnFailedReload { get; init; }
    
    public required Func<Task> ActionOnReloaded { get; init; }
}