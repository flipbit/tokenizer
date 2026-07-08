using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Processes front matter tokens that don't require input text matching.
/// </summary>
internal static class FrontMatterProcessor
{
    /// <summary>
    /// Iterates template tokens and evaluates values for any front matter tokens,
    /// recording matches on the result.
    /// </summary>
    public static void Process(
        Template template,
        TokenizeResult result,
        DecoratorPipeline pipeline,
        FileLocation location)
    {
        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (pipeline.Evaluate(token, string.Empty, location, out var evaluatedValue))
            {
                if (pipeline.Collector.IsEnabled)
                {
                    pipeline.Collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                        tokenName: token.Name, tokenId: token.Id,
                        value: evaluatedValue?.ToString());
                }
                if (evaluatedValue != null)
                {
                    result.Tokens.AddMatch(token, evaluatedValue, token.Location);
                }
            }
            else
            {
                if (pipeline.Collector.IsEnabled)
                {
                    pipeline.Collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                        tokenName: token.Name, tokenId: token.Id);
                }
            }
        }
    }
}
