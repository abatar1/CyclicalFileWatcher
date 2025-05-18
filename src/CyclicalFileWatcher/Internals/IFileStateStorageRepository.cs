using System;
using System.Collections.Generic;
using FileWatcher.Base;

namespace FileWatcher.Internals;

internal interface IFileStateStorageRepository<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    List<IFileStateStorage<TFileStateContent>> GetAll();

    bool TryGetValue(FileStateIdentifier identifier, out IFileStateStorage<TFileStateContent> value);

    void Set(FileStateIdentifier identifier, IFileWatcherParameters<TFileStateContent> fileWatcherParameters);
}