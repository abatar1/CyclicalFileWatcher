using FileWatcher;

namespace CyclicalFileWatcher.UnitTests;

internal sealed class StringContent : IFileStateContent
{
    public required string Content { get; init; }
    
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}