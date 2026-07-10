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
        IDiagnosticCollector collector)
    {
        if (!string.IsNullOrEmpty(definition.Value))
        {
            var setContext = new TokenDecoratorContext(typeof(SetTransformer), decoratorCache);
            setContext.AddParameter(definition.Value);
            token.AddDecorator(setContext);

            if (collector.IsEnabled)
            {
                collector.RecordCompilation(CompilationEventType.DecoratorApplied,
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

    private static bool TryApplyConcatenation(string tokenName, DecoratorDefinition decorator, Token token, IDiagnosticCollector collector)
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
            collector.RecordCompilation(CompilationEventType.ConcatenationApplied,
                tokenName: tokenName,
                detail: token.ConcatenationString ?? "(empty)");
        }

        return true;
    }

    private static bool TryApplyTransformer(DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var transformerType in registry.Transformers)
        {
            if (string.Equals(decorator.Name, transformerType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Transformer", transformerType.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                if (decorator.IsNotDecorator)
                {
                    throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                }

                var context = new TokenDecoratorContext(transformerType, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.RecordCompilation(CompilationEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: transformerType.Name,
                        decoratorArgs: decorator.Args.ToArray());
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryApplyValidator(DecoratorDefinition decorator, Token token,
        DecoratorRegistry registry, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache,
        IDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var validatorType in registry.Validators)
        {
            if (string.Equals(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var context = new TokenDecoratorContext(validatorType, decoratorCache);
                foreach (var arg in decorator.Args)
                {
                    context.AddParameter(arg);
                }

                context.IsNotValidator = decorator.IsNotDecorator;
                token.AddDecorator(context);

                if (collector.IsEnabled)
                {
                    collector.RecordCompilation(CompilationEventType.DecoratorApplied,
                        tokenName: token.Name,
                        decoratorName: validatorType.Name,
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
