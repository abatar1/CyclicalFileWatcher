using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileWatcher.Internals;

internal sealed class FileWatchProcessor<TFileStateContent>(
    IFileStateManagerConfiguration configuration,
    IFileSubscriptionTrigger<TFileStateContent> trigger,
    ReadWriteLockProvider lockProvider,
    IFileStateStorageRepository<TFileStateContent> repository)
    where TFileStateContent : IFileStateContent
{
    public async Task CreateWatchingTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var tasks = repository.GetAll().Select(async state =>
            {
                using var _ = await lockProvider.AcquireWriterLockAsync(state.Identifier, cancellationToken);
                        
                var file = await TryUpdateFileStateAsync(state, cancellationToken);
                if (file == null)
                    return;
                       
                await TryTriggerSubscriptionAsync(file, cancellationToken);
            });

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (AggregateException e)
            {
                await ProcessFileUpdateExceptionAsync(e);
                throw;
            }
            finally
            {
                await Task.Delay(configuration.FileCheckInterval, cancellationToken);
            }
        }
    }
    
    private async Task<FileState<TFileStateContent>?> TryUpdateFileStateAsync(IFileStateStorage<TFileStateContent> fileStateStorage, CancellationToken cancellationToken)
    {
        try
        {
            var fileHasChanged = await fileStateStorage.HasChangedAsync(cancellationToken);
            if (!fileHasChanged)
                return null;
                            
            var file = await fileStateStorage.GetLatestAsync(cancellationToken);
            await configuration.ActionOnFileReloaded.Invoke(file.Identifier);
            return file;
        }
        catch (Exception e)
        {
            throw new FileWatcherReloadException(fileStateStorage.Identifier, e);
        }
    }
    
    private async Task TryTriggerSubscriptionAsync(FileState<TFileStateContent> file, CancellationToken cancellationToken)
    {
        try
        {
            await trigger.TriggerSubscriptionsAsync(file, cancellationToken);
            await configuration.ActionOnSubscribeAction.Invoke(file.Identifier);
        }
        catch (Exception e)
        {
            throw new FileWatcherSubscriptionException(file.Identifier, e);
        }
    }
    
    private async Task ProcessFileUpdateExceptionAsync(AggregateException e)
    {
        var exceptions = e.Flatten().InnerExceptions
            .ToList();
        foreach (var exception in exceptions)
        {
            switch (exception)
            {
                case FileWatcherReloadException fileWatcherReloadException:
                    await configuration.ActionOnFileReloadFailed.Invoke(fileWatcherReloadException);
                    break;
                case FileWatcherSubscriptionException fileWatcherSubscriptionException:
                    await configuration.ActionOnSubscribeActionFailed.Invoke(fileWatcherSubscriptionException);
                    break;
            }
        }
    }
}