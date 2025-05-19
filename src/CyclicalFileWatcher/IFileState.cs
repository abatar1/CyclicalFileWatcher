using System;

namespace FileWatcher;

/// <summary>
/// Represents the state of a file, including its identifier, last modified timestamp, and content.
/// </summary>
/// <typeparam name="TFileStateContent">
/// The type representing the content of the file's state. Must implement <see cref="IFileStateContent"/>.
/// </typeparam>
public interface IFileState<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    /// <summary>
    /// Gets the unique identifier for the IFileState.
    /// </summary>
    /// <remarks>
    /// The identifier is used to identify an observable file by its path.
    /// </remarks>
    FileStateIdentifier Identifier { get; init; }

    /// <summary>
    /// Gets the key used to identify the unique file's state.
    /// </summary>
    /// <remarks>
    /// The identifier is used to distinguish historical states of the same files.
    /// </remarks>
    string Key { get; init; }

    /// <summary>
    /// Gets the timestamp indicating the coordinated universal time (UTC) when the file was last modified.
    /// </summary>
    /// <remarks>
    /// This property provides the last modified timestamp of the file in UTC to track changes to the file's state.
    /// </remarks>
    DateTime ModifiedAtUtc { get; init; }

    /// <summary>
    /// Gets the content of the file's state.
    /// </summary>
    /// <remarks>
    /// The content represents the content data associated with the current state of the file.
    /// </remarks>
    TFileStateContent Content { get; init; }
}