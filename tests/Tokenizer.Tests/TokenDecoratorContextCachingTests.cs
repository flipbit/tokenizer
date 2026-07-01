using Tokens.Transformers;
using Xunit;

namespace Tokens
{
    public class TokenDecoratorContextCachingTests
    {
        [Fact]
        public void GivenSameDecoratorType_WhenCreatingMultipleDecorators_ThenReturnsSameInstance()
        {
            var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer));
            var context2 = new TokenDecoratorContext(typeof(ToLowerTransformer));

            var decorator1 = context1.CreateDecorator();
            var decorator2 = context2.CreateDecorator();

            Assert.Same(decorator1, decorator2);
        }

        [Fact]
        public void GivenDifferentDecoratorTypes_WhenCreatingDecorators_ThenReturnsDifferentInstances()
        {
            var context1 = new TokenDecoratorContext(typeof(ToLowerTransformer));
            var context2 = new TokenDecoratorContext(typeof(ToUpperTransformer));

            var decorator1 = context1.CreateDecorator();
            var decorator2 = context2.CreateDecorator();

            Assert.NotSame(decorator1, decorator2);
        }
    }
}
