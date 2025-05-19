using FileWatcher;

namespace CyclicalFileWatcher.Test;

public sealed class FileObject : IFileStateContent
{
    public required string Content { get; init; }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}