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
    public async Task GivenTextReaderEnumerator_WhenFillBufferAsyncCalled_ThenRefillsBuffer()
    {
        // Arrange — large input that requires multiple fills
        var input = new string('a', 1200); // > DefaultBufferSize (1024)
        var enumerator = new TokenEnumerator(new StringReader(input));

        // Consume the initial buffer contents
        for (var i = 0; i < 1024; i++)
        {
            enumerator.Next();
        }

        // Act — explicitly refill via async path
        await enumerator.FillBufferAsync(CancellationToken.None);

        // Assert — remaining characters are now available
        Assert.False(enumerator.IsEmpty);
        Assert.Equal('a', enumerator.Peek());
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
    public void GivenNeedsRefillProperty_WhenBufferLowAndReaderExhausted_ThenReturnsFalse()
    {
        // Arrange — short input fully buffered. Reader exhaustion is discovered
        // lazily (when a Read returns 0), so drain the buffer first.
        var enumerator = new TokenEnumerator(new StringReader("hi"));
        enumerator.Next(); // 'h'
        enumerator.Next(); // 'i'

        // Force exhaustion discovery via IsEmpty (triggers a FillBuffer that returns 0)
        Assert.True(enumerator.IsEmpty);

        // Act / Assert — reader now known to be exhausted
        Assert.False(enumerator.NeedsRefill);
    }

    [Fact]
    public void GivenNeedsRefillProperty_WhenBufferLowAndReaderHasData_ThenReturnsTrue()
    {
        // Arrange — large input, buffer has some data but reader has more
        var input = new string('x', 2000);
        var enumerator = new TokenEnumerator(new StringReader(input));

        // Consume most of the buffer to get below watermark
        for (var i = 0; i < 900; i++)
        {
            enumerator.Next();
        }

        // Act / Assert — buffer is below watermark and reader has more data
        Assert.True(enumerator.NeedsRefill);
    }

    [Fact]
    public void GivenValueLongerThanBuffer_WhenTryMatch_ThenGrowsBufferAndMatches()
    {
        // Arrange — value longer than DefaultBufferSize (1024) forces GrowBuffer
        var longValue = new string('x', 1500);
        var enumerator = new TokenEnumerator(new StringReader(longValue + " tail"));

        // Act / Assert — TryMatch must grow buffer to hold 1500 chars
        Assert.True(enumerator.TryMatch(longValue));
        Assert.False(enumerator.TryMatch(" tail")); // at start, not at tail _position
    }

    [Fact]
    public void GivenValueDoublingBuffer_WhenTryMatch_ThenGrowsBufferMultipleTimes()
    {
        // Arrange — value > 2048 forces multiple GrowBuffer calls (1024 → 2048 → 4096)
        var longValue = new string('y', 3000);
        var enumerator = new TokenEnumerator(new StringReader(longValue));

        // Act / Assert
        Assert.True(enumerator.TryMatch(longValue));
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

    [Fact]
    public void GivenNonBufferedReader_WhenPeekReturnsMinus1_ThenDoesNotTruncateInput()
    {
        // Arrange — reader whose Peek() always returns -1 (simulates NetworkStream-backed StreamReader)
        var input = "hello world";
        var reader = new NonBufferedTextReader(input);
        var enumerator = new TokenEnumerator(reader);

        // Act — consume all characters
        var result = new char[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            result[i] = enumerator.Next();
        }

        // Assert — all characters consumed, not truncated by premature Peek()-based exhaustion
        Assert.Equal(input, new string(result));
        Assert.Equal(input.Length, enumerator.CharactersConsumed);
        // Peek triggers a FillBuffer that discovers the reader is exhausted
        Assert.Equal('\0', enumerator.Peek());
        Assert.True(enumerator.IsEmpty);
    }
}
