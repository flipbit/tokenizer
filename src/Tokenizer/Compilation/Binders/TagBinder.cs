using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;

namespace Tokens.Compilation.Binders;

internal static class TagBinder
{
    public static void Bind(TemplateDefinition definition, Template template, IDiagnosticCollector collector)
    {
        foreach (var tag in definition.Tags)
        {
            if (template.Tags.Any(t => t == tag))
                continue;

            template.AddTag(tag);

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.TagAdded, detail: tag);
            }
        }
    }
}
