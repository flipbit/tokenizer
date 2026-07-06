using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Creates Template instances from parsed TemplateDefinitions. Owns auto-naming via an incrementing counter.
/// </summary>
internal static class TemplateFactory
{
    private static int _templateCounter;

    public static Template Create(ulong id, TemplateDefinition definition)
    {
        var template = new Template(id, definition.Options);

        template.Name = string.IsNullOrWhiteSpace(definition.Name)
            ? $"Template_{Interlocked.Increment(ref _templateCounter)}"
            : definition.Name;

        return template;
    }
}
