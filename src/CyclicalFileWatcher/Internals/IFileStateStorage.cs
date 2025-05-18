using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal interface IFileStateStorage<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    FileStateIdentifier Identifier { get; }
    
    Task<FileState<TFileStateContent>> GetAsync(string key, CancellationToken cancellationToken);

    Task<FileState<TFileStateContent>> GetLatestAsync(CancellationToken cancellationToken);

    Task<bool> HasChangedAsync(CancellationToken cancellationToken);
}