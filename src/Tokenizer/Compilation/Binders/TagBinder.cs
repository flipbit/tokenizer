using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Assigns tags from a TemplateDefinition to a Template, skipping duplicates.
/// </summary>
internal static class TagBinder
{
    public static void Bind(TemplateDefinition definition, Template template, ICompilationDiagnosticCollector collector)
    {
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var tag in definition.Tags)
        {
            if (template.Tags.Any(t => string.Equals(t, tag, StringComparison.Ordinal)))
                continue;

            template.AddTag(tag);

            if (collector.IsEnabled)
            {
                collector.Record(CompilationEventType.TagAdded, detail: tag);
            }
        }
    }
}
