using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy that uses string.Contains() on the raw input string to find hints.
/// Does not touch the enumerator, so no reset is needed.
/// Only used on the sync path where rawInput is always available.
/// </summary>
internal class ContainsHintStrategy : IHintStrategy
{
    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0)
        {
            return false;
        }

        if (rawInput == null)
        {
            throw new ArgumentNullException(nameof(rawInput), "ContainsHintStrategy requires rawInput — use IntegratedHintStrategy for streaming inputs");
        }

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
        // ContainsHintStrategy uses upfront scanning, not per-token tracking — no-op
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        return false;
    }
}
