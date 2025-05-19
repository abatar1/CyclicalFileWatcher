using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal sealed class FileStateStorageRepository<TFileStateContent>(IFileSystemProxy fileSystemProxy)
    : IFileStateStorageRepository<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly ConcurrentDictionary<FileStateIdentifier, IFileStateStorage<TFileStateContent>> _storages = new();

    public List<IFileStateStorage<TFileStateContent>> GetAll()
    {
        return _storages.Values.ToList();
    }

    public bool TryGetValue(FileStateIdentifier identifier, out IFileStateStorage<TFileStateContent> value)
    {
        return _storages.TryGetValue(identifier, out value);
    }

    public void Set(FileStateIdentifier identifier, IFileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        _storages.GetOrAdd(identifier, new FileStateStorage<TFileStateContent>(fileWatcherParameters, fileSystemProxy));
    }
    
    internal void Set(FileStateIdentifier identifier, IFileStateStorage<TFileStateContent> storage)
    {
        _storages.GetOrAdd(identifier, storage);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var storage in _storages.Values)
            await storage.DisposeAsync();
    }
}