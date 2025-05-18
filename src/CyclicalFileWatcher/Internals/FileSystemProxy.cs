using System;
using System.IO;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal sealed class FileSystemProxy : IFileSystemProxy
{
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
    
    public DateTime GetLastWriteTimeUtc(string filePath)
    {
        return File.GetLastWriteTimeUtc(filePath);
    }
    
    public async Task<bool> CheckFileCanBeLoadedAsync(string filePath)
    {
        long streamLength;
        try
        {
            await using var inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            streamLength = inputStream.Length;
        }
        catch (SystemException)
        {
            return false;
        }

        return streamLength > 0;
    }
}