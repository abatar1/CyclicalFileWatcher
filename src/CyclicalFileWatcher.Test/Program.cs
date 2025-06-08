using CyclicalFileWatcher.Test;
using FileWatcher;

var filePath = args[0];
const int fileCheckSecondsInterval = 5;
const int depth = 3;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    cts.Dispose();
};

var configuration = new FileStateManagerConfiguration
{
    FileCheckInterval = TimeSpan.FromSeconds(fileCheckSecondsInterval),
    ActionOnFileReloaded = async id => await Task.Run(() => Console.WriteLine("FileWatcher reload complete for file {0}", id.FilePath)),
    ActionOnFileReloadFailed = async ex => await Task.Run(() => Console.WriteLine("FileWatcher reload failure for file {0}", ex.FileIdentifier.FilePath)),
    ActionOnSubscribeAction = async id => await Task.Run(() => Console.WriteLine("FileWatcher subscribe action complete for file {0}", id.FilePath)),
    ActionOnSubscribeActionFailed = async ex => await Task.Run(() => Console.WriteLine("FileWatcher subscribe failure for file {0}", ex.FileIdentifier.FilePath))
};
var fileWatcher = new CyclicalFileWatcher<FileObject>(configuration);
var parameters = new FileWatcherParameters<FileObject>
{
    FilePath = filePath,
    Depth = depth,
    FileStateContentFactory = async path => new FileObject {Content = await File.ReadAllTextAsync(path, cts.Token) },
    FileStateKeyFactory = async (_, _) => await Task.FromResult(Guid.NewGuid().ToString())
};
await fileWatcher.WatchAsync(parameters, cts.Token);
await fileWatcher.SubscribeAsync(filePath,  async _ => await Task.Run(() => Console.WriteLine("Executing subscription action")), cts.Token);

Console.WriteLine($"Started testing for filepath {filePath}");

try
{
    while (!cts.IsCancellationRequested)
    {
        _ = await fileWatcher.GetLatestAsync(filePath, cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(fileCheckSecondsInterval), cts.Token);
    }
}
catch (TaskCanceledException)
{
    // Expected to be canceled.
    Console.WriteLine($"Testing for filepath {filePath} cancelled, finishing test.");
}

