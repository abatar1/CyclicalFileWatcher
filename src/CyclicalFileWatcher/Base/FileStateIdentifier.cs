namespace FileWatcher;

/// <summary>
/// Represents a unique identifier for a file's state.
/// </summary>
/// <param name="FilePath">
/// The file path that uniquely identifies the state of the file.
/// </param>
public sealed record FileStateIdentifier(string FilePath);