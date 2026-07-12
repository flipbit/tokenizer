using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for PreambleNeverFound issues by searching the input text
/// for near-matches of the missed token's preamble (case-insensitive, whitespace-normalised,
/// or substring containment).
/// </summary>
internal sealed partial class PreambleNearMissHintGenerator : IHintGenerator
{
#if NET8_0_OR_GREATER
#pragma warning disable MA0009 // GeneratedRegex does not support matchTimeout; source-generated regex avoids ReDoS
    [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
#pragma warning restore MA0009
#else
    private static readonly Regex WhitespaceRegexInstance = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static Regex WhitespaceRegex() => WhitespaceRegexInstance;
#endif

    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   TokenizationEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.PreambleNeverFound)
            return null;

        var preamble = sourceEvent.Detail ?? sourceEvent.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(preamble))
            return null;

        var inputContent = trace.InputContent;

        if (string.IsNullOrEmpty(inputContent))
            return null;

        var normalizedPreamble = NormalizeWhitespace(preamble);

        if (trace.CachedInputLines == null)
        {
            trace.CachedInputLines = inputContent!.Split('\n');
        }
        var lines = trace.CachedInputLines;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var normalizedLine = NormalizeWhitespace(line);
            var lineNumber = i + 1;

            if (normalizedLine.Equals(normalizedPreamble, StringComparison.OrdinalIgnoreCase))
            {
                return $"Input contains '{line.Trim()}' at line {lineNumber.ToInvariant()} (case difference). Update template preamble to match.";
            }

            if (normalizedLine.IndexOf(normalizedPreamble, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Input contains '{line.Trim()}' at line {lineNumber.ToInvariant()} (case difference). Update template preamble to match.";
            }
        }

        return null;
    }

    private static string NormalizeWhitespace(string value)
    {
        return WhitespaceRegex().Replace(value.Trim(), " ");
    }
}
