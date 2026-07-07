using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Tokenization;

/// <summary>
/// Handles assignment of matched values to target objects via the token's decorator pipeline.
/// Session-scoped: constructed once per tokenization session with shared options and diagnostics.
/// </summary>
internal sealed class TokenAssigner
{
    private readonly TokenizerOptions _options;
    private readonly IDiagnosticCollector _collector;

    internal TokenAssigner(TokenizerOptions options, IDiagnosticCollector collector)
    {
        _options = options;
        _collector = collector;
    }

    internal IDiagnosticCollector Collector => _collector;

    /// <summary>
    /// Prepares the value, runs the decorator pipeline, and assigns the result to the target object.
    /// </summary>
    internal bool Assign(Token token, object? target, string value, FileLocation location, out object? assignedValue)
    {
        assignedValue = null;

        var prepared = PrepareValue(token, value);
        if (prepared == null) return false;

        if (_options.TrimTrailingWhiteSpace)
        {
            prepared = prepared.TrimEnd();
        }

        if (!RunDecoratorPipeline(token, prepared, location, out assignedValue)) return false;

        if (target is IDictionary<string, object> dictionary)
        {
            return SetDictionaryValue(token, dictionary, assignedValue!);
        }

        // Target can be null if not reflecting onto an object
        if (target is null)
        {
            return true;
        }

        try
        {
            if (token.CanConcatenate)
            {
                if (assignedValue == null) return true;

                var current = target.GetValue(token.Name);

                if (current == null)
                {
                    // First assignment — no existing value to concatenate with, set directly
                    target.SetValue(token.Name, assignedValue!, StringComparison.Ordinal);
                }
                else if (ValueConcatenation.CanConcatenate(current, assignedValue))
                {
                    var concatenated = ValueConcatenation.Concatenate(current, assignedValue, token.ConcatenationString);
                    if (concatenated != null) target.SetValue(token.Name, concatenated, StringComparison.Ordinal);
                }
                else
                {
                    throw new TokenAssignmentException(token, $"Unable to concatenate type {assignedValue.GetType().Name} to {token.Name}");
                }
            }
            else
            {
                target.SetValue(token.Name, assignedValue!, StringComparison.Ordinal);
            }
        }
        catch (MissingMemberException)
        {
            if (!_options.IgnoreMissingProperties)
            {
                throw;
            }

            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: token.Name, tokenId: token.Id,
                    value: value,
                    detail: $"Property '{token.Name}' not found on target type; ignored via IgnoreMissingProperties");
            }
        }
        catch (TypeConversionException ex)
        {
            _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                tokenName: token.Name, tokenId: token.Id,
                value: value,
                detail: $"Type conversion failed: {ex.Message}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Dry-run: checks whether the value can pass through preparation and the decorator pipeline
    /// without performing any assignment.
    /// </summary>
    internal bool CanAssign(Token token, string value)
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

    private bool RunDecoratorPipeline(Token token, object input, FileLocation? location, out object? assignedValue)
    {
        assignedValue = input;

        foreach (var decorator in token.Decorators)
        {
            if (decorator.IsTransformer)
            {
                if (!decorator.TryTransform(assignedValue!, out var output))
                {
                    _collector.Record(DiagnosticEventType.TransformerFailed,
                        tokenName: token.Name, tokenId: token.Id,
                        location: location,
                        value: assignedValue?.ToString(),
                        decoratorName: decorator.DecoratorType.Name,
                        decoratorArgs: decorator.Parameters.ToArray());

                    return false;
                }

                _collector.Record(DiagnosticEventType.TransformerSucceeded,
                    tokenName: token.Name, tokenId: token.Id,
                    location: location,
                    value: assignedValue?.ToString(),
                    detail: output?.ToString(),
                    decoratorName: decorator.DecoratorType.Name,
                    decoratorArgs: decorator.Parameters.ToArray());

                assignedValue = output;
            }

            if (decorator.IsValidator)
            {
                if (decorator.Validate(assignedValue!))
                {
                    _collector.Record(DiagnosticEventType.ValidatorPassed,
                        tokenName: token.Name, tokenId: token.Id,
                        value: assignedValue?.ToString(),
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

    private static bool SetDictionaryValue(Token token, IDictionary<string, object> dictionary, object input)
    {
        if (token.IsRepeating)
        {
            List<object> list;
            if (dictionary.ContainsKey(token.Name))
            {
                list = dictionary[token.Name] as List<object> ?? new List<object> { dictionary[token.Name] };
            }
            else
            {
                list = new List<object>();
            }
            list.Add(input);
            input = list;
        }

        if (dictionary.ContainsKey(token.Name))
        {
            dictionary[token.Name] = input;
        }
        else
        {
            dictionary.Add(token.Name, input);
        }

        return true;
    }
}
