using System.Collections.Concurrent;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Transformers;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Resolves decorator definitions against a DecoratorRegistry and creates TokenDecoratorContext instances on the target Token.
/// </summary>
internal static class DecoratorBinder
{
    public static void Bind(TokenDefinition definition, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        ICompilationDiagnosticCollector collector)
    {
        if (!string.IsNullOrEmpty(definition.Value))
        {
            var setContext = new TokenDecoratorContext(typeof(SetTransformer), () => new SetTransformer(), decoratorCache);
            setContext.AddParameter(definition.Value);
            token.AddDecorator(setContext);

            if (collector.IsEnabled)
            {
                collector.Record(CompilationEventType.DecoratorApplied,
                    tokenName: token.Name,
                    decoratorName: nameof(SetTransformer),
                    detail: definition.Value);
            }
        }

        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var decorator in definition.Decorators)
        {
            if (TryApplyConcatenation(definition.Name ?? string.Empty, decorator, token, collector))
                continue;

            if (TryApplyTransformer(decorator, token, registry, decoratorCache, collector))
                continue;

            if (TryApplyValidator(decorator, token, registry, decoratorCache, collector))
                continue;

            throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
        }

        ValidateFrontMatterToken(definition, token);
    }

    private static bool TryApplyConcatenation(string tokenName, DecoratorDefinition decorator, Token token, ICompilationDiagnosticCollector collector)
    {
        if (!string.Equals("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase))
            return false;

        if (decorator.Args.Count > 1)
        {
            throw new TokenizerException($"Token '{tokenName}' Concat() must have a single argument.");
        }

        token.CanConcatenate = true;

        if (decorator.Args.Count == 1)
        {
            token.ConcatenationString = decorator.Args[0];
        }

        if (collector.IsEnabled)
        {
            collector.Record(CompilationEventType.ConcatenationApplied,
                tokenName: tokenName,
                detail: token.ConcatenationString ?? "(empty)");
        }

        return true;
    }

    private static bool TryApplyTransformer(DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        ICompilationDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var registration in registry.Transformers)
        {
            if (string.Equals(decorator.Name, registration.Type.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Transformer", registration.Type.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                if (decorator.IsNotDecorator)
                {
                    throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                }

                var context = new TokenDecoratorContext(registration.Type, registration.Factory, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.Record(CompilationEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: registration.Type.Name,
                        decoratorArgs: decorator.Args.ToArray());
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryApplyValidator(DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        ICompilationDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var registration in registry.Validators)
        {
            if (string.Equals(decorator.Name, registration.Type.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Validator", registration.Type.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var context = new TokenDecoratorContext(registration.Type, registration.Factory, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                context.IsNotValidator = decorator.IsNotDecorator;
                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.Record(CompilationEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: registration.Type.Name,
                        decoratorArgs: decorator.Args.ToArray());
                }

                return true;
            }
        }

        return false;
    }

    private static void ValidateFrontMatterToken(TokenDefinition definition, Token token)
    {
        if (definition.IsFrontMatterToken)
        {
            var hasSetTransformer = token.Decorators.Any(d => d.DecoratorType == typeof(SetTransformer));
            if (!hasSetTransformer)
            {
                throw new TokenizerException($"Front Matter Token '{definition.Name}' must have an assignment operation.");
            }
        }
    }
}
