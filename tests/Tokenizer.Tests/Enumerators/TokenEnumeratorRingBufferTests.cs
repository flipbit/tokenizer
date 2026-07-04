using System.IO;
using Xunit;

namespace Tokens.Enumerators;

public class TokenEnumeratorRingBufferTests
{
    [Fact]
    public void GivenTextReaderEnumerator_WhenFillBufferCalled_ThenBuffersCharacters()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("hello world"));

        // Act — constructor calls FillBuffer, so data is buffered

        // Assert
        Assert.Equal('h', enumerator.Peek());
        Assert.Equal('h', enumerator.Next());
        Assert.Equal('e', enumerator.Next());
        Assert.False(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenShortInput_WhenFullyDrained_ThenIsEmptyIsTrue()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("hi"));

        // Act
        enumerator.Next(); // 'h'
        enumerator.Next(); // 'i'

        // Assert
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenTextReaderEnumerator_WhenTryMatchAfterFillBuffer_ThenMatchesFromBuffer()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("hello world"));

        // Act / Assert
        Assert.True(enumerator.TryMatch("hello"));
        Assert.False(enumerator.TryMatch("world"));
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task GivenTextReaderEnumerator_WhenFillBufferAsyncCalled_ThenBuffersCharacters()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("async test"));

        // Act / Assert — constructor already filled buffer
        Assert.Equal('a', enumerator.Peek());
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('s', enumerator.Next());
    }

    [Fact]
    public async Task GivenCancelledToken_WhenFillBufferAsync_ThenThrowsOperationCancelled()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("test"));
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => enumerator.FillBufferAsync(cts.Token).AsTask());
    }
#endif

    [Fact]
    public void GivenInputWithCRLF_WhenBuffered_ThenNormalizesToLF()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("a\r\nb"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('\n', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenInputWithLoneCR_WhenBuffered_ThenNormalizesToLF()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("a\rb"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('\n', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenLargeInput_WhenBufferNeedsRefill_ThenTransparentlyRefills()
    {
        // Arrange — create input larger than default buffer size (1024)
        var input = new string('x', 2000);
        var enumerator = new TokenEnumerator(new StringReader(input));

        // Act — consume all characters
        var count = 0;
        while (!enumerator.IsEmpty)
        {
            var c = enumerator.Next();
            Assert.Equal('x', c);
            count++;
        }

        // Assert
        Assert.Equal(2000, count);
        Assert.Equal(2000, enumerator.CharactersConsumed);
    }

    [Fact]
    public void GivenLargeInput_WhenTryMatchAtBufferBoundary_ThenMatchesCorrectly()
    {
        // Arrange — create input where the match spans the buffer refill point
        var prefix = new string('a', 1020);
        var target = "hello";
        var enumerator = new TokenEnumerator(new StringReader(prefix + target + " world"));

        // Act — advance past the prefix
        for (var i = 0; i < 1020; i++)
        {
            enumerator.Next();
        }

        // Assert — TryMatch should work even if buffer needed refill
        Assert.True(enumerator.TryMatch("hello"));
        Assert.False(enumerator.TryMatch("world"));
    }

    [Fact]
    public void GivenNeedsRefillProperty_WhenBufferLow_ThenReportsCorrectly()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("hi"));

        // Act / Assert — short input is fully buffered, reader exhausted
        Assert.False(enumerator.NeedsRefill);
    }

    [Fact]
    public void GivenFillBufferMethod_WhenCalledExplicitly_ThenDoesNotThrow()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("test"));

        // Act / Assert — calling FillBuffer again after constructor should be safe
        enumerator.FillBuffer();
        Assert.Equal('t', enumerator.Peek());
    }
}
