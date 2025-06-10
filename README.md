<div align="center">

# CyclicalFileWatcher <br> :smirk_cat:

> A library for cyclical asynchronous file watching with subscription support.

[![Latest release](https://github.com/abatar1/CyclicalFileWatcher/actions/workflows/main.yml/badge.svg)](https://github.com/abatar1/CyclicalFileWatcher/actions/workflows/main.yml)
![Coveralls](https://img.shields.io/coverallsCoverage/github/abatar1/CyclicalFileWatcher?label=Test%20coverage&link=https%3A%2F%2Fcoveralls.io%2Fgithub%2Fabatar1%CyclicalFileWatcher)
![NuGet Version](https://img.shields.io/nuget/v/CyclicalFileWatcher?label=NuGet%20version&color=white&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%CyclicalFileWatcher)

</div>

## Why CyclicalFileWatcher

Unlike another file watching solutions that rely on OS-level tools or native file system event hooks, **CyclicalFileWatcher** uses a simple polling mechanism to detect changes.  

This approach:

- Works consistently across platforms and environments (e.g. Docker, network drives, cloud volumes).
- Requires no external dependencies, system utilities, or file system support.
- Gives you full control over polling intervals and error handling.

It’s ideal for scenarios where reliability and isolation from the underlying OS are more important than real-time change detection.

## Usage

```csharp
var fileWatcher = new CyclicalFileWatcher<FileObject>(configuration);

await fileWatcher.WatchAsync(parameters, cancellationToken);

await fileWatcher.SubscribeAsync(filePath, async _ =>
{
    Console.WriteLine("Subscription action executed");
}, cancellationToken);

var latestFile = await fileWatcher.GetLatestAsync(filePath, cancellationToken);
```

See the full example in [src/CyclicalFileWatcher.Test/Program.cs](https://github.com/abatar1/CyclicalFileWatcher/blob/main/src/CyclicalFileWatcher.Test/Program.cs)

## License

Released under [MIT](LICENSE) by [@EvgenyHalzov](https://github.com/abatar1).

- You can freely modify and reuse.
- The _original license_ must be included with copies of this software.
- Please _link back_ to this repo if you use a significant portion the source code.
