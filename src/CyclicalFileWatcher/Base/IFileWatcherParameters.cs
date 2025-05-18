namespace FileWatcher.Base;

public interface IFileWatcherParameters<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    string FilePath { get; init; }
    
    int Depth { get; init; }
    
    FileStateContentFactory<TFileStateContent> FileStateContentFactory { get; init; }
    
    FileStateKeyFactory<TFileStateContent> FileStateKeyFactory { get; init; }
}