using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CompileAsyncTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public CompileAsyncTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public async Task GivenTextReader_WhenCompileAsync_ThenProducesValidTemplate()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}, Age: {Age}");

        // Act
        var template = (await _tokenizer.CompileAsync(reader)).Template;

        // Assert
        Assert.NotNull(template);
        Assert.Equal(2, template.Tokens.Count);
    }

    [Fact]
    public async Task GivenStream_WhenCompileAsync_ThenProducesValidTemplate()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Value: {Value}"));

        // Act
        var template = (await _tokenizer.CompileAsync(stream, Encoding.UTF8)).Template;

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public async Task GivenPreCancelledToken_WhenCompileAsync_ThenThrowsOperationCancelled()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _tokenizer.CompileAsync(reader, cts.Token));
    }

    [Fact]
    public async Task GivenTextReader_WhenCompileAsync_ThenProducesSameResultAsSync()
    {
        // Arrange
        var pattern = "Hello {Name}, welcome to {Place}!";
        var syncTemplate = _tokenizer.Compile(pattern).Template;
        using var reader = new StringReader(pattern);

        // Act
        var asyncTemplate = (await _tokenizer.CompileAsync(reader)).Template;

        // Assert
        Assert.Equal(syncTemplate.Tokens.Count, asyncTemplate.Tokens.Count);
        var syncTokens = syncTemplate.Tokens.ToList();
        var asyncTokens = asyncTemplate.Tokens.ToList();
        for (var i = 0; i < syncTokens.Count; i++)
        {
            Assert.Equal(syncTokens[i].Name, asyncTokens[i].Name);
            Assert.Equal(syncTokens[i].Preamble, asyncTokens[i].Preamble);
        }
    }
}
