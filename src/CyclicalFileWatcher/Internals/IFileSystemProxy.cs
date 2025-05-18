using System;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal interface IFileSystemProxy
{
    bool FileExists(string filePath);
    
    DateTime GetLastWriteTimeUtc(string filePath);
    
    Task<bool> CheckFileCanBeLoadedAsync(string filePath);
}