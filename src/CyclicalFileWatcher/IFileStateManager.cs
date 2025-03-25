using System;
using System.Threading;
using System.Threading.Tasks;
using CyclicalFileWatcher.Base;

namespace CyclicalFileWatcher;

public interface IFileStateManager<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    Task WatchAsync(FileWatcherParameters<TFileStateContent> parameters, CancellationToken cancellationToken);

    Task<IFileState<TFileStateContent>> GetAsync(string filePath, string fileKey, CancellationToken cancellationToken);

    Task<IFileState<TFileStateContent>> GetLatestAsync(string filePath, CancellationToken cancellationToken);
}