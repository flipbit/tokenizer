using System.IO;
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

    [Fact]
    public void GivenTextReader_WhenCompiling_ThenReturnsTemplateWithTokens()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");

        // Act
        var template = tokenizer.Compile(reader);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Tokens);
    }

    [Fact]
    public void GivenTextReaderAndName_WhenCompiling_ThenTemplateHasExplicitName()
    {
        // Arrange
        using var reader = new StringReader("Name: {Name}");

        // Act
        var template = tokenizer.Compile(reader, "reader-template");

        // Assert
        Assert.Equal("reader-template", template.Name);
    }

    [Fact]
    public void GivenSamePatternCompiledTwice_WhenUsingStringOverload_ThenCacheReturnsSameTemplate()
    {
        // Arrange
        const string pattern = "Name: {Name}";

        // Act
        var t1 = tokenizer.Compile(pattern);
        var t2 = tokenizer.Compile(pattern);

        // Assert
        Assert.Same(t1, t2);
    }

    [Fact]
    public void GivenTextReaderCompilation_WhenCompiledTwice_ThenCacheIsNotUsed()
    {
        // Arrange & Act
        var t1 = tokenizer.Compile(new StringReader("Name: {Name}"));
        var t2 = tokenizer.Compile(new StringReader("Name: {Name}"));

        // Assert
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void GivenTokenizer_WhenClearingCache_ThenNextCompileReturnsNewInstance()
    {
        // Arrange
        const string pattern = "Name: {Name}";
        var t1 = tokenizer.Compile(pattern);

        // Act
        tokenizer.ClearCompilationCache();
        var t2 = tokenizer.Compile(pattern);

        // Assert
        Assert.NotSame(t1, t2);
    }

    [Fact]
    public void GivenCachingDisabled_WhenCompilingSamePattern_ThenReturnsNewInstanceEachTime()
    {
        // Arrange
        var noCacheTokenizer = CreateTokenizer(new TokenizerOptions { CompilationCacheMaxSize = 0 });
        const string pattern = "Name: {Name}";

        // Act
        var t1 = noCacheTokenizer.Compile(pattern);
        var t2 = noCacheTokenizer.Compile(pattern);

        // Assert
        Assert.NotSame(t1, t2);
    }
}
