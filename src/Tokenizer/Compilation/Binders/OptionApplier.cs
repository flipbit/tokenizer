using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Applies template-level option overrides to individual tokens.
/// </summary>
internal static class OptionApplier
{
    public static void Apply(Token token, TokenizerOptions options, IDiagnosticCollector collector)
    {
        if (options.OutOfOrderTokens)
        {
            token.IsOptional = true;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.OptionApplied,
                    tokenName: token.Name,
                    detail: "OutOfOrderTokens: marked as optional");
            }
        }

        if (!token.TerminateOnNewLine && options.TerminateOnNewLine)
        {
            token.TerminateOnNewLine = true;

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.OptionApplied,
                    tokenName: token.Name,
                    detail: "TerminateOnNewLine: applied from global option");
            }
        }
    }
}
