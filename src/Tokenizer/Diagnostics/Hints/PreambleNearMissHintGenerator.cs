using System;
using System.Text.RegularExpressions;

namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for PreambleNeverFound issues by searching the input text
/// for near-matches of the missed token's preamble (case-insensitive, whitespace-normalised,
/// or substring containment).
/// </summary>
internal sealed class PreambleNearMissHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   TokenizationDiagnostics trace)
    {
        if (issue.Type != DiagnosticIssueType.PreambleNeverFound)
            return null;

        var preamble = sourceEvent.Detail ?? sourceEvent.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(preamble))
            return null;

        var inputContent = trace.InputContent;

        if (string.IsNullOrEmpty(inputContent))
            return null;

        var normalizedPreamble = NormalizeWhitespace(preamble);
        var lines = inputContent.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var normalizedLine = NormalizeWhitespace(line);
            var lineNumber = i + 1;

            if (normalizedLine.Equals(normalizedPreamble, StringComparison.OrdinalIgnoreCase))
            {
                return $"Input contains '{line.Trim()}' at line {lineNumber} (case difference). " +
                       $"Update template preamble to match.";
            }

            if (normalizedLine.IndexOf(normalizedPreamble, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Input contains '{line.Trim()}' at line {lineNumber} (case difference). " +
                       $"Update template preamble to match.";
            }
        }

        return null;
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
