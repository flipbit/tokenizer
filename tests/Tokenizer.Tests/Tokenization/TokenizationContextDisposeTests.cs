using Tokens.Tokenization;
using Xunit;

namespace Tokens.Tokenization;

public class TokenizationContextDisposeTests
{
    [Fact]
    public void GivenTokenizationContext_WhenDisposed_ThenCanBeDisposedAgainWithoutError()
    {
        var context = new TokenizationContext();
        context.Initialize("test input");
        context.Dispose();
        context.Dispose();
    }

    [Fact]
    public void GivenTokenizationContext_WhenUsedInUsingBlock_ThenDisposesCleanly()
    {
        using (var context = new TokenizationContext())
        {
            context.Initialize("test input");
        }
    }
}
