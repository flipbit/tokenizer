using Xunit;

namespace Tokens.Tokenization.Integration;

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
