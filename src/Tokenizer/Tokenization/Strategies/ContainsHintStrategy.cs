using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy that uses string.Contains() on the raw input string to find hints.
/// Does not touch the enumerator, so no reset is needed.
/// When raw input is unavailable (TextReader inputs), automatically falls back to
/// single-pass integrated hint tracking via <see cref="IntegratedHintStrategy"/>.
/// </summary>
internal class ContainsHintStrategy : IHintStrategy
{
    private readonly IntegratedHintStrategy fallback = new();
    private bool usingFallback;

    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0)
        {
            usingFallback = false;
            return false;
        }

        if (rawInput == null)
        {
            usingFallback = true;
            return fallback.PreProcess(template, enumerator, rawInput, result, collector);
        }

        usingFallback = false;

        foreach (var hint in template.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (rawInput.Contains(hint.Text))
            {
                result.Hints.AddMatch(hint, enumerator);

                collector.Record(DiagnosticEventType.HintMatched,
                    value: hint.Text,
                    location: enumerator.Location);
            }
        }

        foreach (var hint in template.Hints)
        {
            result.Hints.AddMiss(hint);

            if (hint.Optional == false &&
                result.Hints.Misses.Any(m => m.Text == hint.Text))
            {
                collector.Record(DiagnosticEventType.HintMissing,
                    value: hint.Text);
            }
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        if (usingFallback)
        {
            fallback.OnTokenMatched(token);
        }
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        if (usingFallback)
        {
            return fallback.PostProcess(result);
        }

        return false;
    }
}
