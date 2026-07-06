using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

internal static class TokenBinder
{
    public static void Bind(TemplateDefinition definition, Template template,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        foreach (var tokenDef in definition.Tokens)
        {
            var token = TokenFactory.Create(tokenDef, template.Options, collector);
            OptionApplier.Apply(token, template.Options, collector);
            DecoratorBinder.Bind(tokenDef, token, registry, decoratorCache, collector);
            template.AddToken(token);
            RepeatingTokenLinker.Link(token, template, collector);
        }
    }
}
