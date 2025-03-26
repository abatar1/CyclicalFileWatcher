using System.Threading;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal interface IFileSubscriptionTrigger<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    Task TriggerSubscriptionsAsync(FileState<TFileStateContent> fileState, CancellationToken cancellationToken);
}