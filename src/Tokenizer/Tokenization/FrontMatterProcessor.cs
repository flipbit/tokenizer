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
        TokenAssigner assigner,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (assigner.Assign(token, targetObject, string.Empty, location, out var assignedValue))
            {
                if (assigner.Collector.IsEnabled)
                {
                    assigner.Collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                        tokenName: token.Name, tokenId: token.Id,
                        value: assignedValue?.ToString());
                }
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                if (assigner.Collector.IsEnabled)
                {
                    assigner.Collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                        tokenName: token.Name, tokenId: token.Id);
                }
            }
        }
    }
}
