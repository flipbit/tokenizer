using Xunit;

#pragma warning disable MA0048 // Scenario test: TokenizationContext.Constructor.Tests.cs
namespace Tokens.Tokenization;

public class ServiceConstructorTests
{
    [Fact]
    public void GivenTokenizationContext_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var context = new TokenizationContext();

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TokenizationContext>(context);
        Assert.NotNull(context.Candidates);
        Assert.NotNull(context.Replacement);
        Assert.NotNull(context.MatchIds);
        Assert.NotNull(context.DisabledRepeatingTokens);
        Assert.NotNull(context.ReplacementLocation);
    }
}
