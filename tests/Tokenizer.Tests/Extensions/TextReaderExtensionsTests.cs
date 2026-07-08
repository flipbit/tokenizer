using Tokens.Exceptions;
using Xunit;

namespace Tokens.Extensions;

public class TextReaderExtensionsTests
{
    [Fact]
    public async Task GivenShortInput_WhenReadToEndBoundedAsync_ThenReturnsFullContent()
    {
        // Arrange
        using var reader = new StringReader("Hello World");

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None);

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task GivenInputExceedingMaxLength_WhenReadToEndBoundedAsync_ThenThrowsTokenizerException()
    {
        // Arrange
        var longInput = new string('x', 200);
        using var reader = new StringReader(longInput);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<TokenizerException>(
            () => reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None));
        Assert.Contains("exceeds maximum allowed length", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenZeroMaxLength_WhenReadToEndBoundedAsync_ThenReadsWithoutLimit()
    {
        // Arrange
        var longInput = new string('x', 10_000);
        using var reader = new StringReader(longInput);

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 0, CancellationToken.None);

        // Assert
        Assert.Equal(longInput, result);
    }

    [Fact]
    public async Task GivenEmptyReader_WhenReadToEndBoundedAsync_ThenReturnsEmptyString()
    {
        // Arrange
        using var reader = new StringReader(string.Empty);

        // Act
        var result = await reader.ReadToEndBoundedAsync(maxLength: 100, CancellationToken.None);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GivenCancelledToken_WhenReadToEndBoundedAsync_ThenThrowsOperationCancelled()
    {
        // Arrange
        var longInput = new string('x', 10_000);
        using var reader = new StringReader(longInput);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => reader.ReadToEndBoundedAsync(maxLength: 0, cts.Token));
    }
}
