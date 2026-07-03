using Xunit;

namespace Tokens.Transformers;

public class RegexReplaceTransformerTests
{
    private readonly RegexReplaceTransformer transformer = new();

    [Fact]
    public void GivenMatchingPattern_WhenTransforming_ThenReplacesMatches()
    {
        // Act
        var result = transformer.TryTransform("abc123def456", [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("abc#def#", transformed);
    }

    [Fact]
    public void GivenNonMatchingPattern_WhenTransforming_ThenReturnsOriginal()
    {
        // Act
        var result = transformer.TryTransform("hello", [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenPatternWithCaptureGroup_WhenTransforming_ThenUsesGroupInReplacement()
    {
        // Act
        var result = transformer.TryTransform("2026-07-02", [@"(\d{4})-(\d{2})-(\d{2})", "$2/$3/$1"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("07/02/2026", transformed);
    }

    [Fact]
    public void GivenPatternWithInlineCaseFlag_WhenTransforming_ThenRespectsCaseFlag()
    {
        // Act
        var result = transformer.TryTransform("Hello HELLO hello", ["(?i)hello", "hi"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hi hi hi", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(null!, [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenCatastrophicBacktrackingPattern_WhenTransforming_ThenThrowsRegexMatchTimeoutException()
    {
        // Arrange — (a+)+$ is a classic ReDoS pattern; this input causes catastrophic backtracking
        var input = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaab";

        // Act & Assert
        Assert.Throws<System.Text.RegularExpressions.RegexMatchTimeoutException>(
            () => transformer.TryTransform(input, [@"(a+)+$", ""], out var _));
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", null!, out var t));
    }

    [Fact]
    public void GivenOnlyOneArg_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", [@"\d+"], out var t));
    }
}
