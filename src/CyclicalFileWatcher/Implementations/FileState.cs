using System;
using System.Threading.Tasks;

namespace CyclicalFileWatcher.Implementations;

internal sealed class FileState<TFileStateContent> : IFileState<TFileStateContent>, IAsyncDisposable
    where TFileStateContent : IFileStateContent
{ 
    public required string FilePath { get; init; }
    
    public required string Key { get; init; }
    
    public required DateTime ModifiedAtUtc { get; init; }
    
    public required TFileStateContent Content { get; init; }
    
    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
    }
}