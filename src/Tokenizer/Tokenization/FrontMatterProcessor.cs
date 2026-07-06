using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Processes front matter tokens that don't require input text matching.
/// </summary>
internal static class FrontMatterProcessor
{
    /// <summary>
    /// Iterates template tokens and assigns values for any front matter tokens.
    /// </summary>
    public static void Process(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue, collector))
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                    tokenName: token.Name, tokenId: token.Id,
                    value: assignedValue?.ToString());
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                    tokenName: token.Name, tokenId: token.Id);
            }
        }
    }
}
