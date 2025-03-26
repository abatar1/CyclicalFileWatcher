using System;
using System.Threading.Tasks;

namespace FileWatcher.Base;

public sealed class FileWatcherParameters<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    public required string FilePath { get; init; }
    
    public required int Depth { get; init; }
    
    public required Func<Task<TFileStateContent>> FileStateContentFactory { get; init; }
    
    public required Func<TFileStateContent, Task<string>> FileStateKeyFactory { get; init; }
}