namespace FileWatcher.Base;

/// <summary>
/// Defines the parameters required for configuring and operating a file watcher.
/// </summary>
/// <typeparam name="TFileStateContent">
/// The type of the file state content associated with this file watcher configuration.
/// Must implement the <see cref="IFileStateContent"/> interface.
/// </typeparam>
public interface IFileWatcherParameters<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    /// <summary>
    /// Gets or initializes the file path to monitor. The file path specifies the exact location
    /// of the file or directory that will be observed for changes or accessed for state management.
    /// </summary>
    string FilePath { get; init; }

    /// <summary>
    /// Gets or initializes the depth value used to define a limit on the number of file states
    /// retained during monitoring or management. This value determines how many past states
    /// are preserved before older states are removed or overwritten.
    /// </summary>
    int Depth { get; init; }

    /// <summary>
    /// Represents a delegate that defines a factory method for creating instances of
    /// <typeparamref name="TFileStateContent"/> from a given file path.
    /// It encapsulates the logic required to produce a file state content object
    /// by asynchronously processing the provided file path.
    /// </summary>
    /// <typeparam name="TFileStateContent">
    /// The type of the content that implements the <see cref="IFileStateContent"/> interface.
    /// </typeparam>
    /// <param name="filePath">The path of the file used to generate the file state content.</param>
    FileStateContentFactory<TFileStateContent> FileStateContentFactory { get; init; }

    /// <summary>
    /// Represents a delegate function used to generate a unique key for a file's state.
    /// The factory function accepts the file path and the corresponding state content
    /// and returns a task resulting in the computed key as a string. This key serves as
    /// a unique identifier for managing and accessing file states history.
    /// </summary>
    FileStateKeyFactory<TFileStateContent> FileStateKeyFactory { get; init; }
}