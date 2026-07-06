using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CompileApiTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    public CompileApiTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPattern_WhenCompiling_ThenReturnsTemplateWithTokens()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var template = tokenizer.Compile(pattern);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenPatternAndName_WhenCompiling_ThenTemplateHasExplicitName()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var template = tokenizer.Compile(pattern, "my-template");

        // Assert
        Assert.Equal("my-template", template.Name);
    }

}
