using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

internal static class HintBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
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
