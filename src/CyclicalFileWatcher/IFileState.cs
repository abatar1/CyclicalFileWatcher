using System;

namespace FileWatcher;

public interface IFileState<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    FileStateIdentifier Identifier { get; init; }
    
    DateTime ModifiedAtUtc { get; init; }
    
    TFileStateContent Content { get; init; }
}