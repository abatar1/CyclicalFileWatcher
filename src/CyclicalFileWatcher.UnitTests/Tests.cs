using FileWatcher;
using Moq;
using Xunit;

namespace CyclicalFileWatcher.UnitTests;

public sealed class Tests
{
    [Fact]
    public void EmptyTest()
    {
        var configurationMock = new Mock<IFileStateManagerConfiguration>();
        
        var _ = new CyclicalFileWatcher<StringContent>(configurationMock.Object);
        
        Assert.True(true);
    }
}