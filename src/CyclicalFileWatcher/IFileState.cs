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
    /// Gets the unique identifier for the file's state.
    /// </summary>
    /// <remarks>
    /// The identifier is used to distinguish the current state of a file and is represented by a
    /// <see cref="FileStateIdentifier"/> containing the file path.
    /// </remarks>
    FileStateIdentifier Identifier { get; init; }

    /// <summary>
    /// Gets the timestamp indicating the coordinated universal time (UTC) when the file was last modified.
    /// </summary>
    /// <remarks>
    /// This property provides the last modified timestamp of the file in UTC to track changes to the file's state
    /// accurately and ensure synchronization across different environments.
    /// </remarks>
    DateTime ModifiedAtUtc { get; init; }

    /// <summary>
    /// Gets the content of the file's state.
    /// </summary>
    /// <remarks>
    /// The content represents the data associated with the current state of the file
    /// and is of a type that implements <see cref="IFileStateContent"/>.
    /// </remarks>
    TFileStateContent Content { get; init; }
}