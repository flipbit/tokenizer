namespace Tokens.Diagnostics
{
    internal static class DiagnosticSummaryBuilder
    {
        public static DiagnosticSummary Build(TokenizationDiagnostics diagnostics)
        {
            return new DiagnosticSummary { Verdict = "Diagnostics collected." };
        }
    }
}
