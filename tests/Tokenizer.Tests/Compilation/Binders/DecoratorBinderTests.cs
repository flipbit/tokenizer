using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class DecoratorBinderTests
{
    private readonly DecoratorRegistry _registry = new(new TokenizerOptions());
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    [Fact]
    public void GivenTokenDefinitionWithValue_WhenBinding_ThenSetTransformerIsAdded()
    {
        var definition = new TokenDefinition { Content = "{Foo}" };
        definition.AppendName("Foo");
        definition.AppendValue("bar");
        var token = new Token("Foo", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.Equal(typeof(SetTransformer), token.Decorators[0].DecoratorType);
        Assert.Equal("bar", token.Decorators[0].Parameters[0]);
    }

    [Fact]
    public void GivenTransformerDecorator_WhenBinding_ThenTransformerIsApplied()
    {
        var definition = new TokenDefinition { Content = "{Date}" };
        definition.AppendName("Date");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("ToDateTime");
        decorator.Args.Add("yyyy-MM-dd");
        definition.Decorators.Add(decorator);
        var token = new Token("Date", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.Equal(typeof(ToDateTimeTransformer), token.Decorators[0].DecoratorType);
        Assert.Equal("yyyy-MM-dd", token.Decorators[0].Parameters[0]);
    }

    [Fact]
    public void GivenValidatorDecorator_WhenBinding_ThenValidatorIsApplied()
    {
        var definition = new TokenDefinition { Content = "{Amount}" };
        definition.AppendName("Amount");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("IsNumeric");
        definition.Decorators.Add(decorator);
        var token = new Token("Amount", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.Equal(typeof(IsNumericValidator), token.Decorators[0].DecoratorType);
    }

    [Fact]
    public void GivenNotValidator_WhenBinding_ThenIsNotValidatorIsSet()
    {
        var definition = new TokenDefinition { Content = "{Amount}" };
        definition.AppendName("Amount");
        var decorator = new DecoratorDefinition { IsNotDecorator = true };
        decorator.AppendName("IsNumeric");
        definition.Decorators.Add(decorator);
        var token = new Token("Amount", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.True(token.Decorators[0].IsNotValidator);
    }

    [Fact]
    public void GivenNotTransformer_WhenBinding_ThenThrowsTokenizerException()
    {
        var definition = new TokenDefinition { Content = "{Date}" };
        definition.AppendName("Date");
        var decorator = new DecoratorDefinition { IsNotDecorator = true };
        decorator.AppendName("ToDateTime");
        decorator.Args.Add("yyyy-MM-dd");
        definition.Decorators.Add(decorator);
        var token = new Token("Date", "", new FileLocation());

        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("cannot be prefixed with '!'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenConcatDecorator_WhenBinding_ThenTokenCanConcatenate()
    {
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        decorator.Args.Add(", ");
        definition.Decorators.Add(decorator);
        var token = new Token("Items", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.True(token.CanConcatenate);
        Assert.Equal(", ", token.ConcatenationString);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenConcatWithNoArgs_WhenBinding_ThenConcatenationStringIsNull()
    {
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        definition.Decorators.Add(decorator);
        var token = new Token("Items", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.True(token.CanConcatenate);
        Assert.Null(token.ConcatenationString);
    }

    [Fact]
    public void GivenConcatWithTooManyArgs_WhenBinding_ThenThrowsTokenizerException()
    {
        var definition = new TokenDefinition { Content = "{Items}" };
        definition.AppendName("Items");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("concat");
        decorator.Args.Add(", ");
        decorator.Args.Add("extra");
        definition.Decorators.Add(decorator);
        var token = new Token("Items", "", new FileLocation());

        Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance));
    }

    [Fact]
    public void GivenUnknownDecorator_WhenBinding_ThenThrowsTokenizerException()
    {
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendName("Token");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("NonExistentDecorator");
        definition.Decorators.Add(decorator);
        var token = new Token("Token", "", new FileLocation());

        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("Unknown Token Operation", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenFrontMatterTokenWithoutSetTransformer_WhenBinding_ThenThrowsTokenizerException()
    {
        var definition = new TokenDefinition
        {
            Content = "{Decorator}",
            IsFrontMatterToken = true,
        };
        definition.AppendName("Decorator");
        var token = new Token("Decorator", "", new FileLocation());
        token.IsFrontMatterToken = true;

        var ex = Assert.Throws<TokenizerException>(() =>
            DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance));
        Assert.Contains("must have an assignment operation", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenFrontMatterTokenWithSetTransformer_WhenBinding_ThenSucceeds()
    {
        var definition = new TokenDefinition
        {
            Content = "{Foo}",
            IsFrontMatterToken = true,
        };
        definition.AppendName("Foo");
        definition.AppendValue("bar");
        var token = new Token("Foo", "", new FileLocation());
        token.IsFrontMatterToken = true;

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.Equal(typeof(SetTransformer), token.Decorators[0].DecoratorType);
    }

    [Fact]
    public void GivenTransformerWithShortName_WhenBinding_ThenTransformerIsResolved()
    {
        var definition = new TokenDefinition { Content = "{Name}" };
        definition.AppendName("Name");
        var decorator = new DecoratorDefinition();
        decorator.AppendName("ToUpper");
        definition.Decorators.Add(decorator);
        var token = new Token("Name", "", new FileLocation());

        DecoratorBinder.Bind(definition, token, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(token.Decorators);
        Assert.True(token.Decorators[0].IsTransformer);
    }
}
