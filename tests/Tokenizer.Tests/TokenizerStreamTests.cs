using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizerStreamTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    private class SimpleRecord
    {
        public string? Name { get; set; }
    }

    public TokenizerStreamTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenTextReaderInput_WhenTokenizing_ThenExtractsValuesCorrectly()
    {
        // Arrange
        var template = tokenizer.Compile("Name: {SimpleRecord.Name}");
        using var reader = new StringReader("Name: Alice");

        // Act
        var result = tokenizer.Tokenize(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.First("SimpleRecord.Name"));
    }

    [Fact]
    public void GivenTextReaderInput_WhenTokenizingGeneric_ThenPopulatesObject()
    {
        // Arrange
        var template = tokenizer.Compile("Name: {SimpleRecord.Name}");
        using var reader = new StringReader("Name: Bob");

        // Act
        var result = tokenizer.Tokenize<SimpleRecord>(template, reader);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", result.Value.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenTokenizing_ThenExtractsValuesCorrectly()
    {
        // Arrange
        var template = tokenizer.Compile("Name: {SimpleRecord.Name}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie"));

        // Act
        var result = tokenizer.Tokenize(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Charlie", result.First("SimpleRecord.Name"));
    }

    [Fact]
    public void GivenStreamInput_WhenTokenizingGeneric_ThenPopulatesObject()
    {
        // Arrange
        var template = tokenizer.Compile("Name: {SimpleRecord.Name}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Diana"));

        // Act
        var result = tokenizer.Tokenize<SimpleRecord>(template, stream, Encoding.UTF8);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Diana", result.Value.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenTokenizationCompletes_ThenStreamIsNotDisposed()
    {
        // Arrange
        var template = tokenizer.Compile("Name: {SimpleRecord.Name}");
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Eve"));

        // Act
        tokenizer.Tokenize(template, stream, Encoding.UTF8);

        // Assert - stream is still usable (not disposed)
        stream.Position = 0;
        Assert.True(stream.CanRead);

        // Cleanup
        stream.Dispose();
    }
}
