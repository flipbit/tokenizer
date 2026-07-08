using Tokens.Builders;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizeResultTests : TokenizerTestBase
{
    public TokenizeResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTokenizeResult_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("test-template").Build();
        var result = new TokenizeResult(template);

        // Act
        var output = result.ToString();

        // Assert
        Assert.Equal("TokenizeResult('test-template': 0 matched, 0 missed)", output);
    }

    [Fact]
    public void GivenTemplateWithOnlyFrontMatterTokens_WhenAllMatched_ThenSuccessIsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var templateContent = "---\nname: Test\nset: Status = Found\nhint: hello\n---\nhello";

        var compiled = tokenizer.Compile(templateContent).Template;

        // Act
        var result = tokenizer.Tokenize(compiled, "hello world");

        // Assert
        Assert.True(result.Success);
    }
}
