using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizerAsyncTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public TokenizerAsyncTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsync_ThenMatchesTokens()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}, Age: {Age}");
        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = await _tokenizer.TokenizeAsync(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Alice", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => m.Token.Name == "Age").Value);
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsyncGeneric_ThenPopulatesObject()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Person.Name}, Age: {Person.Age}");
        using var reader = new StringReader("Name: Bob, Age: 25");

        // Act
        var result = await _tokenizer.TokenizeAsync<Person>(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", result.Value.Name);
        Assert.Equal(25, result.Value.Age);
    }

    [Fact]
    public async Task GivenStream_WhenTokenizeAsync_ThenMatchesTokens()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie"));

        // Act
        var result = await _tokenizer.TokenizeAsync(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Charlie", result.Tokens.Matches.First().Value);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenCancelled_ThenThrowsOperationCancelled()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}");
        using var reader = new StringReader("Name: Test");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _tokenizer.TokenizeAsync(template, reader, cts.Token));
    }

    [Fact]
    public async Task GivenStringInput_WhenAsyncAndSyncTokenize_ThenProducesSameResults()
    {
        // Arrange
        var template = _tokenizer.Compile("Hello {Name}, welcome to {Place}!");
        var input = "Hello World, welcome to Earth!";

        // Act
        var syncResult = _tokenizer.Tokenize(template, input);
        using var reader = new StringReader(input);
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
    public async Task GivenStream_WhenTokenizeAsyncGeneric_ThenPopulatesObject()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Person.Name}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Diana"));

        // Act
        var result = await _tokenizer.TokenizeAsync<Person>(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Diana", result.Value.Name);
    }

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }
}
