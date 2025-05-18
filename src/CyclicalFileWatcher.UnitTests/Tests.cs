using FileWatcher;
using FileWatcher.Base;
using FileWatcher.Internals;
using Moq;
using Xunit;

namespace CyclicalFileWatcher.UnitTests;

public sealed class WatcherTests
{
    [Fact]
    public async Task SimpleRoutineTest()
    {
        // Arrange
        const string filePath = "testFilePath";
        const int depth = 2;
        const string fileContent1 = "testFileContent1";
        const string fileContent2 = "testFileContent2";
        const string fileContent3 = "testFileContent3";
        const string fileKey1 = "key1";
        const string fileKey2 = "key2";
        const string fileKey3 = "key3";
        
        var configurationMock = new Mock<IFileStateManagerConfiguration>();
        configurationMock.SetupGet(x => x.FileCheckInterval).Returns(TimeSpan.FromMilliseconds(1));
        configurationMock.SetupGet(x => x.ActionOnFileReloaded).Returns(_ => Task.CompletedTask);
        configurationMock.SetupGet(x => x.ActionOnSubscribeAction).Returns(_ => Task.CompletedTask);
        
        var fileProxyMock = new Mock<IFileSystemProxy>();
        fileProxyMock
            .Setup(x => x.CheckFileCanBeLoadedAsync(It.Is<string>(y => y == filePath)))
            .ReturnsAsync(true);
        fileProxyMock
            .Setup(x => x.FileExists(It.Is<string>(y => y == filePath)))
            .Returns(true);
        fileProxyMock
            .SetupSequence(x => x.GetLastWriteTimeUtc(filePath))
            .Returns(new DateTime(2022, 1, 2))
            .Returns(new DateTime(2022, 1, 3))
            .Returns(new DateTime(2022, 1, 4));

        var parameters = new Mock<IFileWatcherParameters<StringContent>>();
        parameters.SetupGet(x => x.FilePath).Returns(filePath);
        parameters.SetupGet(x => x.Depth).Returns(depth);
        parameters.SetupSequence(x => x.FileStateKeyFactory)
            .Returns((_, _) => Task.FromResult(fileKey1))
            .Returns((_, _) => Task.FromResult(fileKey2))
            .Returns((_, _) => Task.FromResult(fileKey3));
        parameters.SetupSequence(x => x.FileStateContentFactory)
            .Returns(_ => Task.FromResult(new StringContent { Content = fileContent1 }))
            .Returns(_ => Task.FromResult(new StringContent { Content = fileContent2 }))
            .Returns(_ => Task.FromResult(new StringContent { Content = fileContent3 }));
        
        var storage = new FileStateStorage<StringContent>(parameters.Object, fileProxyMock.Object);
        
        var fileStateStorageRepository = new FileStateStorageRepository<StringContent>(fileProxyMock.Object);
        fileStateStorageRepository.Set(new FileStateIdentifier(filePath), storage);
        
        // Act
        var watcher = new CyclicalFileWatcher<StringContent>(configurationMock.Object, fileStateStorageRepository, fileProxyMock.Object);
        await watcher.WatchAsync(parameters.Object, CancellationToken.None);
        
        // Assert
        // Simulates updating file state.
        await RunUntilKeyFoundAsync(async () =>
        {
            await AssertFileStateAsync(watcher, filePath, fileKey1, fileContent1);
        });
        await RunUntilKeyFoundAsync(async () =>
        {
            await AssertFileStateAsync(watcher, filePath, fileKey2, fileContent2);
        });
        await RunUntilKeyFoundAsync(async () =>
        {
            await AssertFileStateAsync(watcher, filePath, fileKey3, fileContent3);
        });
        
        // Ensures that the first state has already deleted.
        await Assert.ThrowsAsync<KeyNotFoundException>(async () => await watcher.GetAsync(filePath, fileKey1, CancellationToken.None));
        
        // Ensures that the last state is the last added state.
        var latestFileState = await watcher.GetLatestAsync(filePath, CancellationToken.None);
        Assert.Equal(fileKey3, latestFileState.Key);
    }

    private static async Task RunUntilKeyFoundAsync(Func<Task> func)
    {
        while (true)
        {
            try
            {
                await func.Invoke();
                break;
            }
            catch (KeyNotFoundException)
            {
            }
        }
    }

    private static async Task AssertFileStateAsync(CyclicalFileWatcher<StringContent> watcher, string expectedFilePath, string expectedKey, string expectedContent)
    {
        var fileState = await watcher.GetAsync(expectedFilePath, expectedKey, CancellationToken.None);
        Assert.NotNull(fileState);
        Assert.NotNull(fileState.Content);
        Assert.Equal(expectedContent, fileState.Content.Content);
        Assert.Equal(expectedFilePath, fileState.Identifier.FilePath);
    }
}