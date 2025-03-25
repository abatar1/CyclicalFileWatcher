using System;
using System.Threading.Tasks;

namespace CyclicalFileWatcher;

public interface IFileStateManagerConfiguration
{
    TimeSpan FileCheckInterval { get; init; }
    
    Func<Exception, Task> ActionOnFailedReload { get; init; }
    
    Func<Task> ActionOnReloaded { get; init; }
}