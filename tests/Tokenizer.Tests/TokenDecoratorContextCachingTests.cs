using System.Collections.Concurrent;
using Tokens.Transformers;
using Xunit;

namespace Tokens;

public class TokenDecoratorContextCachingTests
{
    [Fact]
    public void GivenSameCache_WhenCreatingMultipleDecoratorsOfSameType_ThenReturnsSameInstance()
    {
        var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
        var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache);
        var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache);

        var decorator1 = context1.CreateDecorator();
        var decorator2 = context2.CreateDecorator();

        Assert.Same(decorator1, decorator2);
    }

    [Fact]
    public void GivenDifferentCaches_WhenCreatingSameDecoratorType_ThenReturnsDifferentInstances()
    {
        var cache1 = new ConcurrentDictionary<Type, ITokenDecorator>();
        var cache2 = new ConcurrentDictionary<Type, ITokenDecorator>();
        var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache1);
        var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), cache2);

        var decorator1 = context1.CreateDecorator();
        var decorator2 = context2.CreateDecorator();

        Assert.NotSame(decorator1, decorator2);
    }
}
