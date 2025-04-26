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
    FileStateIdentifier Identifier { get; init; }
    
    DateTime ModifiedAtUtc { get; init; }
    
    TFileStateContent Content { get; init; }
}