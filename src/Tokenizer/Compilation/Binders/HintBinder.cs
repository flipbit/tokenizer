using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Assigns hints from a TemplateDefinition to a Template, skipping duplicates.
/// </summary>
internal static class HintBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var hint in definition.Hints)
        {
            if (template.Hints.Any(h => h == hint))
                continue;

            template.AddHint(hint);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.HintAdded, detail: hint.Text);
            }
        }
    }
}
