using System;

namespace FileWatcher.Base;

public struct FileSubscription
{
    public required Guid SubscriptionId { get; init; }
    
    public required string FilePath { get; init; }
}