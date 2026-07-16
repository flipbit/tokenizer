using System.Collections.Concurrent;
using Tokens.Transformers;
using Xunit;

namespace Tokens;

public class TokenDecoratorContextCachingTests
{
    [Fact]
    public void GivenFactory_WhenCreatingDecorator_ThenFactoryIsUsed()
    {
        // Arrange
        var callCount = 0;
        Func<ITokenDecorator> factory = () => { callCount++; return new ToLowerTransformer(); };
        var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
        var context = new TokenDecoratorContext(typeof(ToLowerTransformer), factory, cache);

        // Act
        context.CreateDecorator();

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void GivenSameCache_WhenCreatingMultipleDecoratorsOfSameType_ThenReturnsSameInstance()
    {
        // Arrange
        var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
        var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), () => new ToLowerTransformer(), cache);
        var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), () => new ToLowerTransformer(), cache);

        // Act
        var decorator1 = context1.CreateDecorator();
        var decorator2 = context2.CreateDecorator();

        // Assert
        Assert.Same(decorator1, decorator2);
    }

    [Fact]
    public void GivenDifferentCaches_WhenCreatingSameDecoratorType_ThenReturnsDifferentInstances()
    {
        // Arrange
        var cache1 = new ConcurrentDictionary<Type, ITokenDecorator>();
        var cache2 = new ConcurrentDictionary<Type, ITokenDecorator>();
        var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer), () => new ToLowerTransformer(), cache1);
        var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer), () => new ToLowerTransformer(), cache2);

        // Act
        var decorator1 = context1.CreateDecorator();
        var decorator2 = context2.CreateDecorator();

        // Assert
        Assert.NotSame(decorator1, decorator2);
    }
}
