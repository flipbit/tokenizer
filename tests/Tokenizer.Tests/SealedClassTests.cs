using System;
using System.Linq;
using Xunit;

namespace Tokens;

public class SealedClassTests
{
    [Theory]
    [InlineData(typeof(Token))]
    [InlineData(typeof(Template))]
    [InlineData(typeof(Hint))]
    [InlineData(typeof(Match))]
    [InlineData(typeof(HintMatch))]
    [InlineData(typeof(TokenizeResult))]
    [InlineData(typeof(TokenizerOptions))]
    [InlineData(typeof(Tokenizer))]
    [InlineData(typeof(TokenResult))]
    [InlineData(typeof(HintResult))]
    [InlineData(typeof(TokenMatcherResult))]
    [InlineData(typeof(TokenMatcher))]
    [InlineData(typeof(TokenDecoratorContext))]
    public void GivenPublicClass_WhenChecked_ThenIsSealed(Type type)
    {
        Assert.True(type.IsSealed, $"{type.Name} should be sealed");
    }

    [Fact]
    public void GivenAllTransformers_WhenChecked_ThenAreSealed()
    {
        var transformerTypes = typeof(Tokenizer).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
            .Where(t => typeof(Transformers.ITokenTransformer).IsAssignableFrom(t));

        foreach (var type in transformerTypes)
        {
            Assert.True(type.IsSealed, $"Transformer {type.Name} should be sealed");
        }
    }

    [Fact]
    public void GivenAllValidators_WhenChecked_ThenAreSealed()
    {
        var validatorTypes = typeof(Tokenizer).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract)
            .Where(t => typeof(Validators.ITokenValidator).IsAssignableFrom(t));

        foreach (var type in validatorTypes)
        {
            Assert.True(type.IsSealed, $"Validator {type.Name} should be sealed");
        }
    }
}
