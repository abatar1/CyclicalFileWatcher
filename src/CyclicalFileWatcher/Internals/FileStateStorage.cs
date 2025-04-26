using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileWatcher.Base;
using Nito.AsyncEx;

namespace FileWatcher.Internals;

internal sealed class FileStateStorage<TFileStateContent> : IAsyncDisposable
    where TFileStateContent : IFileStateContent
{
    private readonly AsyncLazy<FileWatcherParameters<TFileStateContent>> _fileWatcherParameters;
    private readonly ConcurrentDictionary<string, FileState<TFileStateContent>> _filesStatesByKeys = new();
    private readonly LinkedList<string> _fileStateKeysOrder = new();

    public FileStateStorage(FileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        _fileWatcherParameters = new AsyncLazy<FileWatcherParameters<TFileStateContent>>(async () =>
        {
            await AppendFileStateAsync(fileWatcherParameters);
            return fileWatcherParameters;
        });
        _fileWatcherParameters.Start();
    }

    public async Task<FileState<TFileStateContent>> GetFileStateAsync(string key, CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        if (!_filesStatesByKeys.TryGetValue(key, out var file))
            throw new KeyNotFoundException($"File {parameters.FilePath} with key {key} not found or already expired");
        return file;
    }
    
    public async Task<FileState<TFileStateContent>> GetLatestFileStateAsync(CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        var latestKey = _fileStateKeysOrder.Last?.Value;
        if (latestKey == null)
            throw new InvalidOperationException($"No files added yet, could not retrieve latest file from filepath {parameters.FilePath}, seems like a bug");
        return await GetFileStateAsync(latestKey, cancellationToken);
    } 
    
    public async Task<bool> TryUpdateFileStateAsync(CancellationToken cancellationToken)
    {
        var parameters = await _fileWatcherParameters.Task.WaitAsync(cancellationToken);
        
        FileState<TFileStateContent> latestFileState;
        try
        {
            latestFileState = await GetLatestFileStateAsync(cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
        
        var lastModificationDateTime = GetLastModificationDateTime(parameters.FilePath);
        
        if (latestFileState.ModifiedAtUtc == lastModificationDateTime)
            return false;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await CheckFileCanBeLoadedAsync(parameters.FilePath))
                break;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        await AppendFileStateAsync(parameters);
        
        return true;
    }
    
    private async Task AppendFileStateAsync(FileWatcherParameters<TFileStateContent> fileWatcherParameters)
    {
        var fileStateContent = await fileWatcherParameters.FileStateContentFactory.Invoke(fileWatcherParameters.FilePath);
        var key = await fileWatcherParameters.FileStateKeyFactory.Invoke(fileWatcherParameters.FilePath, fileStateContent);

        var fileState = new FileState<TFileStateContent>
        {
            Identifier = new FileStateIdentifier(fileWatcherParameters.FilePath),
            ModifiedAtUtc = GetLastModificationDateTime(fileWatcherParameters.FilePath),
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
    
    private static async Task<bool> CheckFileCanBeLoadedAsync(string filePath)
    {
        long streamLength;
        try
        {
            await using FileStream inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            streamLength = inputStream.Length;
        }
        catch (SystemException)
        {
            return false;
        }

        if (streamLength <= 0)
            return false;
        
        return true;
    }
    
    private static DateTime GetLastModificationDateTime(string certificatePath)
    {
        var fileInfo = new FileInfo(certificatePath);
        return fileInfo.LastWriteTime;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var storedCertificate in _filesStatesByKeys.Values)
            await storedCertificate.DisposeAsync();
    }
}