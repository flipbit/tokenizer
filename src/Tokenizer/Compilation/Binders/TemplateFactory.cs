using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Binders;

internal static class TemplateFactory
{
    private static int templateCounter;

    public static Template Create(ulong id, TemplateDefinition definition)
    {
        var template = new Template(id, definition.Options);

        template.Name = string.IsNullOrWhiteSpace(definition.Name)
            ? $"Template_{Interlocked.Increment(ref templateCounter)}"
            : definition.Name;

        return template;
    }
}
