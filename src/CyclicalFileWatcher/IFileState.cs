using System;

namespace CyclicalFileWatcher;

public interface IFileState<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    string FilePath { get; init; }
    
    string Key { get; init; }
    
    DateTime ModifiedAtUtc { get; init; }
    
    TFileStateContent Content { get; init; }
}