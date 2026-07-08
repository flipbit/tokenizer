using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Assigns tags from a TemplateDefinition to a Template, skipping duplicates.
/// </summary>
internal static class TagBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
        foreach (var tag in definition.Tags)
        {
            if (template.Tags.Any(t => string.Equals(t, tag, StringComparison.Ordinal)))
                continue;

            template.AddTag(tag);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.TagAdded, detail: tag);
            }
        }
    }
}
