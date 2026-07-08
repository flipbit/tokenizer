using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Hint strategy that uses string.Contains() on the raw input string to find hints.
/// Does not touch the enumerator, so no reset is needed.
/// Only used on the sync path where rawInput is always available.
/// </summary>
internal sealed class UpfrontHintStrategy : IHintStrategy
{
    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResult result, IDiagnosticCollector collector)
    {
        if (template.Hints.Count == 0)
        {
            return false;
        }

        if (rawInput == null)
        {
            throw new ArgumentNullException(nameof(rawInput), "UpfrontHintStrategy requires rawInput — use StreamingHintStrategy for streaming inputs");
        }

        foreach (var hint in template.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (rawInput.Contains(hint.Text, StringComparison.Ordinal))
            {
                result.Hints.TryAddMatch(hint, enumerator);

                collector.Record(DiagnosticEventType.HintMatched,
                    value: hint.Text,
                    location: enumerator.Location);
            }
        }

        foreach (var hint in template.Hints)
        {
            if (result.Hints.TryAddMiss(hint) && !hint.Optional)
            {
                collector.Record(DiagnosticEventType.HintMissing,
                    value: hint.Text);
            }
        }

        return result.Hints.Misses.Any(h => !h.Optional);
    }

    /// <inheritdoc />
    public void OnBufferFilled(char[] buffer, int count)
    {
        // UpfrontHintStrategy uses upfront scanning, not buffer-based tracking — no-op
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResult result)
    {
        return false;
    }
}
