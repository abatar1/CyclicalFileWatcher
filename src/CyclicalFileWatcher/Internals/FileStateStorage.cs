using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace FileWatcher.Internals;

internal sealed class FileStateStorage<TFileStateContent> : IFileStateStorage<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    private readonly AsyncLazy<IFileWatcherParameters<TFileStateContent>> _fileWatcherParameters;
    private readonly Dictionary<string, FileState<TFileStateContent>> _filesStatesByKeys = new();
    private readonly IFileSystemProxy _fileSystemProxy;
    private readonly LinkedList<string> _fileStateKeysOrder = [];
    
    public FileStateIdentifier Identifier { get; }

    public FileStateStorage(IFileWatcherParameters<TFileStateContent> fileWatcherParameters, IFileSystemProxy fileSystemProxy)
    {
        _fileSystemProxy = fileSystemProxy;
        Identifier = new FileStateIdentifier(fileWatcherParameters.FilePath);
        _fileWatcherParameters = new AsyncLazy<IFileWatcherParameters<TFileStateContent>>(async () =>
        {
            await AppendFileStateAsync(fileWatcherParameters);
            return fileWatcherParameters;
        });
        _fileWatcherParameters.Start();
    }

    public async Task<FileState<TFileStateContent>> GetAsync(string key, CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        return GetInternal(key, parameters.FilePath);
    }
    
    public async Task<FileState<TFileStateContent>> GetLatestAsync(CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        var latestKey = _fileStateKeysOrder.Last?.Value;
        if (latestKey == null)
            throw new InvalidOperationException($"No files added yet, could not retrieve latest file from filepath {parameters.FilePath}, seems like a bug");
        
        return GetInternal(latestKey, parameters.FilePath);
    } 
    
    public async Task<bool> HasChangedAsync(CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        IFileState<TFileStateContent> latestFileState;
        try
        {
            latestFileState = await GetLatestAsync(cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
        
        var lastModificationDateTime = _fileSystemProxy.GetLastWriteTimeUtc(parameters.FilePath);
        
        if (latestFileState.ModifiedAtUtc == lastModificationDateTime)
            return false;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await _fileSystemProxy.CheckFileCanBeLoadedAsync(parameters.FilePath))
                break;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        await AppendFileStateAsync(parameters);
        
        return true;
    }
    
    private FileState<TFileStateContent> GetInternal(string key, string filePath)
    {
        if (!_filesStatesByKeys.TryGetValue(key, out var file))
            throw new KeyNotFoundException($"File {filePath} with key {key} not found or already expired");
        return file;
    }  
    
    private async Task AppendFileStateAsync(IFileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        var fileStateContent = await fileWatcherParameters.FileStateContentFactory.Invoke(fileWatcherParameters.FilePath);
        var key = await fileWatcherParameters.FileStateKeyFactory.Invoke(fileWatcherParameters.FilePath, fileStateContent);

        var fileState = new FileState<TFileStateContent>
        {
            Identifier = new FileStateIdentifier(fileWatcherParameters.FilePath),
            ModifiedAtUtc = _fileSystemProxy.GetLastWriteTimeUtc(fileWatcherParameters.FilePath),
            Content = fileStateContent,
            Key = key
        };
        
        _filesStatesByKeys[key] = fileState;
        _fileStateKeysOrder.AddLast(key);

        if (_fileStateKeysOrder.Count > fileWatcherParameters.Depth)
        {
            var oldestKey = _fileStateKeysOrder.First.Value;
            _fileStateKeysOrder.RemoveFirst();
            _filesStatesByKeys.Remove(oldestKey, out var oldestFileState);
            await oldestFileState.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var storedCertificate in _filesStatesByKeys.Values)
            await storedCertificate.DisposeAsync();
    }
}