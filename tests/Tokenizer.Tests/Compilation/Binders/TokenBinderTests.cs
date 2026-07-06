using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenBinderTests
{
    private readonly DecoratorRegistry _registry = new(new TokenizerOptions());
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    [Fact]
    public void GivenDefinitionWithTokens_WhenBinding_ThenTemplateHasTokens()
    {
        var definition = new TemplateDefinition();
        var tokenDef = new TokenDefinition { Content = "{Name}" };
        tokenDef.AppendName("Name");
        tokenDef.AppendPreamble("Preamble: ");
        definition.Tokens.Add(tokenDef);
        var template = new TemplateBuilder().Build();

        TokenBinder.Bind(definition, template, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Single(template.Tokens);
        Assert.Equal("Name", template.Tokens.First().Name);
        Assert.Equal("Preamble: ", template.Tokens.First().Preamble);
    }

    [Fact]
    public void GivenMultipleTokenDefinitions_WhenBinding_ThenAllTokensBound()
    {
        var definition = new TemplateDefinition();
        var td1 = new TokenDefinition { Content = "{First}" };
        td1.AppendName("First");
        td1.AppendPreamble("A: ");
        var td2 = new TokenDefinition { Content = "{Second}" };
        td2.AppendName("Second");
        td2.AppendPreamble("B: ");
        definition.Tokens.Add(td1);
        definition.Tokens.Add(td2);
        var template = new TemplateBuilder().Build();

        TokenBinder.Bind(definition, template, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Equal(2, template.Tokens.Count);
    }

    [Fact]
    public void GivenOutOfOrderOptions_WhenBinding_ThenTokensAreOptional()
    {
        var definition = new TemplateDefinition
        {
            Options = new TokenizerOptions { OutOfOrderTokens = true }
        };
        var tokenDef = new TokenDefinition { Content = "{Name}" };
        tokenDef.AppendName("Name");
        definition.Tokens.Add(tokenDef);
        var template = new TemplateBuilder()
            .WithOptions(new TokenizerOptions { OutOfOrderTokens = true })
            .Build();

        TokenBinder.Bind(definition, template, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.True(template.Tokens.First().IsOptional);
    }

    [Fact]
    public void GivenEmptyDefinition_WhenBinding_ThenTemplateHasNoTokens()
    {
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        TokenBinder.Bind(definition, template, _registry, _decoratorCache, NullDiagnosticCollector.Instance);

        Assert.Empty(template.Tokens);
    }
}
