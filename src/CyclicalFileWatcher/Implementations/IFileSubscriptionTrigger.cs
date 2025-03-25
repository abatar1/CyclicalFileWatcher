using System.Threading;
using System.Threading.Tasks;

namespace CyclicalFileWatcher.Implementations;

internal interface IFileSubscriptionTrigger<TFileStateContent>
    where TFileStateContent : IFileStateContent
{
    Task TriggerSubscriptionsAsync(FileState<TFileStateContent> fileState, CancellationToken cancellationToken);
}