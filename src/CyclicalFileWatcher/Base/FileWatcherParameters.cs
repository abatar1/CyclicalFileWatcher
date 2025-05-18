namespace FileWatcher.Base;

public sealed class FileWatcherParameters<TFileStateContent> : IFileWatcherParameters<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    public required string FilePath { get; init; }
    
    public required int Depth { get; init; }
    
    public required FileStateContentFactory<TFileStateContent> FileStateContentFactory { get; init; }
    
    public required FileStateKeyFactory<TFileStateContent> FileStateKeyFactory { get; init; }
}