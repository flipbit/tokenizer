using Tokens.Compilation.Binders;
using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Parsing;

/// <summary>
/// Experimental parser using the minimal AST pipeline (Phase 1: front matter only).
/// </summary>
/// <remarks>
/// <para>
/// This implementation parses the input into a minimal AST via <see cref="TemplateParser"/>,
/// then binds front matter into a <see cref="TemplateDefinition"/> using <see cref="FrontMatterBinder"/>.
/// Non-front-matter content is not bound in Phase 1.
/// </para>
/// <para>
/// Intent: optional/behind-flag usage to evaluate the AST approach without affecting the
/// default <see cref="TemplateDefinitionParser"/>.
/// </para>
/// </remarks>
internal sealed class AstTemplateDefinitionParser : ITemplateDefinitionParser
{
    public TemplateDefinition Parse(string template)
    {
        return Parse(template, new TokenizerOptions());
    }

    public TemplateDefinition Parse(string template, TokenizerOptions options)
    {
        var parser = new TemplateParser();
        var document = parser.Parse(template);
        var result = new TemplateDefinition { Options = options with { } };
        var binder = new FrontMatterBinder();
        binder.Bind(result, document.FrontMatter);
        // Bind tokens from AST (Phase 2 syntax → definitions)
        var bound = TemplateBinder.Bind(document);
        foreach (var tok in bound.Tokens)
        {
            result.Tokens.Add(tok);
        }
        return result;
    }
}


