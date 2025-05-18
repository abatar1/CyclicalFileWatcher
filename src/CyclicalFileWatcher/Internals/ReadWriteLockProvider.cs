using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace FileWatcher.Internals;

internal sealed class ReadWriteLockProvider
{
    private readonly ConcurrentDictionary<FileStateIdentifier, AsyncReaderWriterLock> _locks = new();
    
    public async Task<IDisposable> AcquireReaderLockAsync(FileStateIdentifier identifier, CancellationToken cancellationToken)
    {
        var watchLock = _locks.GetOrAdd(identifier, _ => new AsyncReaderWriterLock());
        return await watchLock.ReaderLockAsync(cancellationToken);
    }

    public async Task<IDisposable> AcquireWriterLockAsync(FileStateIdentifier identifier, CancellationToken cancellationToken)
    {
        var watchLock = _locks.GetOrAdd(identifier, _ => new AsyncReaderWriterLock());
        return await watchLock.WriterLockAsync(cancellationToken);
    }
}