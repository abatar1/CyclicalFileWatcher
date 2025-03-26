using System;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal sealed class FileState<TFileStateContent> : IFileState<TFileStateContent>, IAsyncDisposable
    where TFileStateContent : IFileStateContent
{ 
    public required FileStateIdentifier Identifier { get; init; }
    
    public required string Key { get; init; }
    
    public required DateTime ModifiedAtUtc { get; init; }
    
    public required TFileStateContent Content { get; init; }
    
    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
    }
}