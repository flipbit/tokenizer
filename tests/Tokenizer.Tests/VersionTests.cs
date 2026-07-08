using Xunit;

namespace Tokens;

public class VersionTests
{
    [Fact]
    public void GivenAssembly_WhenCheckingVersion_ThenVersionIs3()
    {
        // Arrange
        var assembly = typeof(Tokenizer).Assembly;

        // Act
        var version = assembly.GetName().Version;

        // Assert
        Assert.Equal(3, version!.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Build);
    }
}
