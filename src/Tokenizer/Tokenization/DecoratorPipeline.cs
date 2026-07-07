using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Extensions;

namespace Tokens.Tokenization;

/// <summary>
/// Runs the decorator pipeline (transformers + validators) on matched token values.
/// Session-scoped: constructed once per tokenization session with shared options and diagnostics.
/// </summary>
internal sealed class DecoratorPipeline
{
    private readonly TokenizerOptions _options;
    private readonly IDiagnosticCollector _collector;

    internal DecoratorPipeline(TokenizerOptions options, IDiagnosticCollector collector)
    {
        _options = options;
        _collector = collector;
    }

    internal IDiagnosticCollector Collector => _collector;

    /// <summary>
    /// Prepares the value and runs the decorator pipeline (transformers then validators).
    /// Returns true if the value passes all decorators; the evaluated (potentially transformed)
    /// value is returned via <paramref name="evaluatedValue"/>.
    /// </summary>
    internal bool Evaluate(Token token, string value, FileLocation location, out object? evaluatedValue)
    {
        evaluatedValue = null;

        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        if (_options.TrimTrailingWhiteSpace)
        {
            prepared = prepared.TrimEnd();
        }

        if (!RunDecoratorPipeline(token, prepared, location, out evaluatedValue)) return false;

        return true;
    }

    /// <summary>
    /// Dry-run: checks whether the value can pass through preparation and the decorator pipeline.
    /// </summary>
    internal bool CanEvaluate(Token token, string value)
    {
        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        return RunDecoratorPipeline(token, prepared, location: null, out _);
    }

    private static string? PrepareValue(Token token, string value)
    {
        if (string.IsNullOrEmpty(value) && !token.IsFrontMatterToken) return null;
        if (token.IsNull) return null;
        if (string.IsNullOrWhiteSpace(token.Name)) return null;

        value = value.TrimTrailingNewLine();

        if (!string.IsNullOrEmpty(value) && token.TerminateOnNewLine)
        {
#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
            var index = value.IndexOf('\n');
            if (index >= 0)
            {
                value = value.Substring(0, index);
            }
#pragma warning restore MA0001
        }

        return value;
    }

    private bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? evaluatedValue)
    {
        evaluatedValue = input;

        foreach (var decorator in token.Decorators)
        {
            if (decorator.IsTransformer)
            {
                if (!decorator.TryTransform(evaluatedValue!, out var output))
                {
                    _collector.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        location: location,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());

                    return false;
                }

                _collector.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: token.Name, tokenId: token.Id,
                    location: location,
                    value: evaluatedValue?.ToString(),
                    detail: output?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());

                evaluatedValue = output;
            }

            if (decorator.IsValidator)
            {
                if (decorator.Validate(evaluatedValue!))
                {
                    _collector.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: evaluatedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);
                }
                else
                {
                    _collector.Record(DiagnosticEventType.ValidatorFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: input?.ToString(),
                        decoratorName: decorator.DecoratorType.Name);

                    return false;
                }
            }
        }

        return true;
    }
}
