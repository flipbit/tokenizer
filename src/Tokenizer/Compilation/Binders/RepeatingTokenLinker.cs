using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Links repeating tokens to their non-repeating counterpart with the same name.
/// </summary>
internal static class RepeatingTokenLinker
{
    public static void Link(Token token, Template template, IDiagnosticCollector collector)
    {
        if (!token.IsRepeating || token.DependsOnId != -1 || template.Tokens.Count < 2)
            return;

        var previous = template.Tokens.Last(t => t.Id != token.Id);

        if (string.Equals(previous.Name, token.Name, StringComparison.Ordinal) && !previous.IsRepeating)
        {
            token.DependsOnId = previous.Id;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.RepeatingTokenLinked,
                    tokenName: token.Name,
                    tokenId: token.Id,
                    detail: $"Linked to token {previous.Id}");
            }
        }
    }
}
