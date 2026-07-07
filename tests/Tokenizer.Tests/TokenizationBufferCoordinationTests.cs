using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

/// <summary>
/// A TextReader that delivers data in configurable chunk sizes,
/// simulating a network stream or similar source where data arrives incrementally.
/// Forces multiple buffer fills and cooperative yield cycles in the tokenization engine.
/// </summary>
internal class ChunkedTextReader : TextReader
{
    private readonly string _data;
    private int _position;
    private readonly int _chunkSize;

    public ChunkedTextReader(string data, int chunkSize)
    {
        _data = data;
        _chunkSize = chunkSize;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (_position >= _data.Length) return 0;
        var toRead = Math.Min(Math.Min(count, _chunkSize), _data.Length - _position);
        _data.CopyTo(_position, buffer, index, toRead);
        _position += toRead;
        return toRead;
    }

    public override int Peek()
    {
        return _position < _data.Length ? _data[_position] : -1;
    }
}

/// <summary>
/// A TextReader that yields on async reads, exercising real async suspension
/// and resumption in the tokenization engine's cooperative buffering loop.
/// </summary>
internal class YieldingTextReader : TextReader
{
    private readonly string _data;
    private int _position;
    private readonly int _chunkSize;

    public YieldingTextReader(string data, int chunkSize)
    {
        _data = data;
        _chunkSize = chunkSize;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (_position >= _data.Length) return 0;
        var toRead = Math.Min(Math.Min(count, _chunkSize), _data.Length - _position);
        _data.CopyTo(_position, buffer, index, toRead);
        _position += toRead;
        return toRead;
    }

    public override async Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        await Task.Yield();
        return Read(buffer, index, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken ct = default)
    {
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        if (_position >= _data.Length) return 0;
        var toRead = Math.Min(Math.Min(buffer.Length, _chunkSize), _data.Length - _position);
        _data.AsSpan(_position, toRead).CopyTo(buffer.Span);
        _position += toRead;
        return toRead;
    }

    public override int Peek()
    {
        return _position < _data.Length ? _data[_position] : -1;
    }
}

/// <summary>
/// Tests for the cooperative buffering model that coordinates async buffer fills
/// with the synchronous ProcessChunk algorithm. Verifies that token values, preambles,
/// and special token handling work correctly when input arrives in chunks.
/// </summary>
public class TokenizationBufferCoordinationTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public TokenizationBufferCoordinationTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public async Task GivenChunkedReader_WhenTokenValueSpansMultipleYields_ThenAccumulatesCompleteValue()
    {
        // Arrange — value of 2000 chars forces ~4 cooperative yield cycles (chunk 500, watermark 256)
        var template = _tokenizer.Compile("Name: {Name}").Template;
        var longValue = new string('a', 2000);
        var input = "Name: " + longValue;
        using var reader = new ChunkedTextReader(input, chunkSize: 500);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Tokens.Matches);
        Assert.Equal(longValue, result.Tokens.Matches[0].Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenMultipleTokensSpanChunks_ThenAllTokensMatch()
    {
        // Arrange — 3 tokens with values that push preambles across chunk boundaries
        var template = _tokenizer.Compile("A:{First}B:{Second}C:{Third}").Template;
        var v1 = new string('x', 800);
        var v2 = new string('y', 800);
        var v3 = new string('z', 800);
        var input = $"A:{v1}B:{v2}C:{v3}";
        using var reader = new ChunkedTextReader(input, chunkSize: 400);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal(v1, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "First", StringComparison.Ordinal)).Value);
        Assert.Equal(v2, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Second", StringComparison.Ordinal)).Value);
        Assert.Equal(v3, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Third", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenComparedToSyncTokenize_ThenResultsAreIdentical()
    {
        // Arrange — same template and input, exercised through both paths
        var template = _tokenizer.Compile("Name: {Name}, Age: {Age}").Template;
        var longName = new string('q', 1500);
        var input = $"Name: {longName}, Age: 42";

        // Act — sync
        var syncResult = _tokenizer.Tokenize(template, input);

        // Act — async with chunked reader
        using var reader = new ChunkedTextReader(input, chunkSize: 300);
        var asyncResult = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.Equal(syncResult.Success, asyncResult.Success);
        Assert.Equal(syncResult.Tokens.Matches.Count, asyncResult.Tokens.Matches.Count);
        for (var i = 0; i < syncResult.Tokens.Matches.Count; i++)
        {
            Assert.Equal(syncResult.Tokens.Matches[i].Token.Name, asyncResult.Tokens.Matches[i].Token.Name);
            Assert.Equal(syncResult.Tokens.Matches[i].Value, asyncResult.Tokens.Matches[i].Value);
        }
    }

    [Fact]
    public async Task GivenChunkSizeOfOne_WhenTokenizing_ThenProducesCorrectResults()
    {
        // Arrange — extreme: every char is a separate read, forcing maximum fill cycles
        var template = _tokenizer.Compile("Name: {Name}, Age: {Age}").Template;
        var input = "Name: Alice, Age: 30";
        using var reader = new ChunkedTextReader(input, chunkSize: 1);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenVerySmallChunks_WhenLargeInput_ThenProducesCorrectResults()
    {
        // Arrange — chunk size 7, input 3000+ chars — forces hundreds of fill cycles
        var template = _tokenizer.Compile("A:{First}B:{Second}C:{Third}").Template;
        var v1 = new string('a', 1000);
        var v2 = new string('b', 1000);
        var v3 = new string('c', 1000);
        var input = $"A:{v1}B:{v2}C:{v3}";
        using var reader = new ChunkedTextReader(input, chunkSize: 7);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Tokens.Matches.Count);
        Assert.Equal(v1, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "First", StringComparison.Ordinal)).Value);
        Assert.Equal(v2, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Second", StringComparison.Ordinal)).Value);
        Assert.Equal(v3, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Third", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenNewlineTerminatedTokenSpansChunks_ThenAssignsCorrectly()
    {
        // Arrange — newline-terminated Name token with a long value that spans multiple fills
        var template = _tokenizer.Compile("Name: {Name}\nAge: {Age}").Template;
        var longName = new string('n', 800);
        var input = $"Name: {longName}\nAge: 25";
        using var reader = new ChunkedTextReader(input, chunkSize: 500);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal(longName, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("25", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenCRLFSplitAcrossChunks_ThenNormalizesToSingleNewline()
    {
        // Arrange — \r is last char of chunk, \n is first char of next chunk
        // CopyToRingBuffer must peek the reader to detect the \r\n pair
        var template = _tokenizer.Compile("Name: {Name}\nAge: {Age}").Template;
        // Position \r\n at chunk boundary: "Name: " (6) + value (chunkSize - 7) + \r\n
        // With chunk size 20: value = 13 chars, so \r at position 19 (last char of chunk 1)
        var value = new string('v', 13);
        var input = $"Name: {value}\r\nAge: 30";
        using var reader = new ChunkedTextReader(input, chunkSize: 20);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal(value, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenRepeatingTokenSpansChunks_ThenAllOccurrencesMatch()
    {
        // Arrange — repeating token with values distributed across chunk boundaries
        var template = _tokenizer.Compile("Item: {Item*}").Template;
        var items = new List<string>();
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 20; i++)
        {
            var item = $"Value{i:D3}_{new string((char)('a' + i % 26), 50)}";
            items.Add(item);
            sb.Append($"Item: {item}\n");
        }
        var input = sb.ToString();
        using var reader = new ChunkedTextReader(input, chunkSize: 200);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.Equal(20, result.Tokens.Matches.Count);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(items[i], result.Tokens.Matches[i].Value);
        }
    }

    [Fact]
    public async Task GivenChunkedReader_WhenMaxInputLengthExceededMidStream_ThenThrows()
    {
        // Arrange — MaxInputLength = 600, input 1200 chars via chunked reader
        // The limit should be hit after the second FillBufferAsync
        var options = new TokenizerOptions { MaxInputLength = 600 };
        var tokenizer = new Tokenizer(options);
        var template = tokenizer.Compile("Name: {Name}").Template;
        var input = "Name: " + new string('x', 1200);
        using var reader = new ChunkedTextReader(input, chunkSize: 500);

        // Act & Assert
        await Assert.ThrowsAsync<TokenizerException>(
            () => tokenizer.TokenizeAsync(template, reader));
    }

    [Fact]
    public async Task GivenCancelledToken_WhenProcessingChunkedInput_ThenThrows()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}").Template;
        var input = "Name: " + new string('x', 2000);
        using var reader = new ChunkedTextReader(input, chunkSize: 500);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _tokenizer.TokenizeAsync(template, reader, cts.Token));
    }

    [Fact]
    public async Task GivenYieldingReader_WhenTokenizing_ThenProducesCorrectResults()
    {
        // Arrange — YieldingTextReader does Task.Yield() before each read,
        // exercising real async suspension/resumption in the RunAsync loop
        var template = _tokenizer.Compile("A:{First}B:{Second}").Template;
        var v1 = new string('p', 1000);
        var v2 = new string('q', 1000);
        var input = $"A:{v1}B:{v2}";
        using var reader = new YieldingTextReader(input, chunkSize: 400);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal(v1, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "First", StringComparison.Ordinal)).Value);
        Assert.Equal(v2, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Second", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public async Task GivenLongPreamble_WhenPreambleExceedsBufferedData_ThenEnsureBufferedFillsAndMatches()
    {
        // Arrange — 300-char preamble with chunk size 130. After cooperative fills bring
        // bufferedCount to ~260 (above watermark), TryMatch calls EnsureBuffered(300)
        // which triggers an additional sync fill to get the remaining chars.
        var preamble = new string('P', 300);
        var templatePattern = $"{preamble}{{Value}}";
        var template = _tokenizer.Compile(templatePattern).Template;
        var input = $"{preamble}hello";
        using var reader = new ChunkedTextReader(input, chunkSize: 130);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public async Task GivenChunkedReader_WhenTokenSwitchHappensNearYieldPoint_ThenBothTokensAssign()
    {
        // Arrange — first token value fills buffer to near watermark, then second token's
        // preamble appears right as NeedsRefill would trigger
        var template = _tokenizer.Compile("X:{Alpha}Y:{Beta}").Template;
        // With chunk 500: initial fill 500, after consuming 245 chars NeedsRefill triggers.
        // Place "Y:" at ~position 250 so the token switch happens right after a yield.
        var v1 = new string('a', 248); // "X:" (2) + 248 = 250 chars before "Y:"
        var v2 = new string('b', 800);
        var input = $"X:{v1}Y:{v2}";
        using var reader = new ChunkedTextReader(input, chunkSize: 500);

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal(v1, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Alpha", StringComparison.Ordinal)).Value);
        Assert.Equal(v2, result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Beta", StringComparison.Ordinal)).Value);
    }
}
