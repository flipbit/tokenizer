using Tokens.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class StringHashExtensionTests : TokenizerTestBase
{
    public StringHashExtensionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenSameString_WhenComputingHash_ThenReturnsSameValue()
    {
        // Arrange
        const string input = "Name: {Name}";

        // Act
        var hash1 = input.ComputeHash();
        var hash2 = input.ComputeHash();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GivenDifferentStrings_WhenComputingHash_ThenReturnsDifferentValues()
    {
        // Arrange
        const string input1 = "Name: {Name}";
        const string input2 = "Age: {Age}";

        // Act
        var hash1 = input1.ComputeHash();
        var hash2 = input2.ComputeHash();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GivenEmptyString_WhenComputingHash_ThenReturnsConsistentValue()
    {
        // Arrange & Act
        var hash1 = string.Empty.ComputeHash();
        var hash2 = string.Empty.ComputeHash();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GivenString_WhenComputingHash_ThenReturnsNonZero()
    {
        // Arrange & Act
        var hash = "Name: {Name}".ComputeHash();

        // Assert
        Assert.NotEqual(0UL, hash);
    }
}
