using FileWatcher;
using FileWatcher.Internals;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CyclicalFileWatcher.UnitTests;

public sealed class WatcherTests(ITestOutputHelper output)
{
    [Fact]
    public async Task MultipleFilesRoutineTest()
    {
        // Arrange
        const int depth = 2;
        
        const string filePath1 = "testFilePath1";
        var fileContents1 = new[] {"testFileContent11", "testFileContent12", "testFileContent13"};
        var fileKeys1 = new[] { "key11", "key12", "key13" };
        
        const string filePath2 = "testFilePath2";
        var fileContents2 = new[] {"testFileContent21", "testFileContent22", "testFileContent23"};
        var fileKeys2 = new[] { "key21", "key22", "key23" };
        
        var fileProxyMock = new Mock<IFileSystemProxy>();
        SetupFileProxy(fileProxyMock, filePath1);
        SetupFileProxy(fileProxyMock, filePath2);
        
        var fileStateStorageRepository = new FileStateStorageRepository<StringContent>(fileProxyMock.Object);
        var parameters1 = SetRepositoryWithParameters(fileStateStorageRepository, fileProxyMock.Object, depth, filePath1, fileKeys1, fileContents1);
        var parameters2 = SetRepositoryWithParameters(fileStateStorageRepository, fileProxyMock.Object, depth, filePath2, fileKeys2, fileContents2);
        
        var configurationMock = new Mock<IFileStateManagerConfiguration>();
        configurationMock.SetupGet(x => x.FileCheckInterval).Returns(TimeSpan.FromMilliseconds(1));
        configurationMock.SetupGet(x => x.ActionOnFileReloaded).Returns(async x => await Task.Run(() => output.WriteLine($"File {x.FilePath} reloaded")));
        configurationMock.SetupGet(x => x.ActionOnSubscribeAction).Returns(async x => await Task.Run(() => output.WriteLine($"File {x.FilePath} subscription executed")));
        var watcher = new CyclicalFileWatcher<StringContent>(configurationMock.Object, fileStateStorageRepository, fileProxyMock.Object);
        
        // Act
        await watcher.WatchAsync(parameters1, CancellationToken.None);
        await watcher.WatchAsync(parameters2, CancellationToken.None);
        
        // Assert
        // Simulates updating file state.
        await AssertFileAsync(watcher, filePath1, fileKeys1, fileContents1);
        await AssertFileAsync(watcher, filePath2, fileKeys2, fileContents2);
     
        // Ensure watcher does not allow getting an element after disposing.
        await watcher.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => watcher.GetLatestAsync(filePath1, CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => watcher.GetAsync(filePath1, fileKeys1[1], CancellationToken.None));
    }

    private static async Task AssertFileAsync(IFileWatcher<StringContent> watcher, string filePath, string[] fileKeys, string[] fileContents)
    {
        await AssertUntilKeyFoundAsync(watcher, filePath, fileKeys[1], fileContents[1]);
        await AssertUntilKeyFoundAsync(watcher, filePath, fileKeys[2], fileContents[2]);
        
        // Ensures that the first state has already deleted.
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await watcher.GetAsync(filePath, fileKeys[0], CancellationToken.None));
        
        // Ensures that the last state is the last added state.
        var latestFileState = await watcher.GetLatestAsync(filePath, CancellationToken.None);
        Assert.Equal(fileKeys[2], latestFileState.Key);
    }

    private static void SetupFileProxy(Mock<IFileSystemProxy> fileProxyMock, string filePath)
    {
        fileProxyMock
            .Setup(x => x.CheckFileCanBeLoadedAsync(It.Is<string>(y => y == filePath)))
            .ReturnsAsync(true);
        fileProxyMock
            .Setup(x => x.FileExists(It.Is<string>(y => y == filePath)))
            .Returns(true);
        // File proxy mock emulates a sequence of calls of GetLastWriteTimeUtc like if a file modified multiple times.
        fileProxyMock
            .SetupSequence(x => x.GetLastWriteTimeUtc(filePath))
            .Returns(new DateTime(2022, 1, 2))
            .Returns(new DateTime(2022, 1, 3))
            .Returns(new DateTime(2022, 1, 4));
    }

    private static IFileWatcherParameters<StringContent> SetRepositoryWithParameters(
        FileStateStorageRepository<StringContent> fileStateStorageRepository,
        IFileSystemProxy fileProxy,
        int depth, 
        string filePath,
        string[] fileKeys, 
        string[] fileContents)
    {
        var parameters = new Mock<IFileWatcherParameters<StringContent>>();
        parameters.SetupGet(x => x.FilePath).Returns(filePath);
        parameters.SetupGet(x => x.Depth).Returns(depth);
        
        var keysSequentialResult = parameters.SetupSequence(x => x.FileStateKeyFactory);
        foreach (var fileKey in fileKeys)
        {
            keysSequentialResult = keysSequentialResult.Returns(async (_, _) => await Task.FromResult(fileKey));
        }
        
        var contentSequentialResult = parameters.SetupSequence(x => x.FileStateContentFactory);
        foreach (var fileContent in fileContents)
        {
            contentSequentialResult = contentSequentialResult.Returns(async _ => await Task.FromResult(new StringContent { Content = fileContent }));
        }
        
        var storage = new FileStateStorage<StringContent>(parameters.Object, fileProxy);
        fileStateStorageRepository.Set(new FileStateIdentifier(filePath), storage);
        return parameters.Object;
    }
    
    private static async Task AssertUntilKeyFoundAsync(IFileWatcher<StringContent> watcher, string expectedFilePath, string expectedKey, string expectedContent)
    {
        while (true)
        {
            try
            {
                var fileState = await watcher.GetAsync(expectedFilePath, expectedKey, CancellationToken.None);
                Assert.NotNull(fileState);
                Assert.NotNull(fileState.Content);
                Assert.Equal(expectedContent, fileState.Content.Content);
                Assert.Equal(expectedFilePath, fileState.Identifier.FilePath);
                break;
            }
            catch (KeyNotFoundException)
            {
            }
        }
    }
}