using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Orchestrates per-token compilation by delegating to focused sub-components.
/// </summary>
internal static class TokenBinder
{
    public static void Bind(TemplateDefinition definition, Template template,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        ICompilationDiagnosticCollector collector)
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
