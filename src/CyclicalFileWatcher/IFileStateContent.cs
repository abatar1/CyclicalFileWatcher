using System;

namespace FileWatcher;

/// <summary>
/// Represents the base interface for defining the structure of file state content.
/// Implementations of this interface are required to provide mechanisms for handling
/// asynchronous disposal to manage resources effectively.
/// </summary>
public interface IFileStateContent : IAsyncDisposable;