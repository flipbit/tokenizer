using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Parsing;

/// <summary>
/// Parses a raw template string into a <see cref="Definitions.TemplateDefinition"/> ready for binding.
/// </summary>
public interface ITemplateDefinitionParser
{
    /// <summary>
    /// Parses the template string and constructs a <see cref="TemplateDefinition"/>.
    /// </summary>
    TemplateDefinition Parse(string template);

    /// <summary>
    /// Parses the template string and constructs a <see cref="TemplateDefinition"/>.
    /// </summary>
    TemplateDefinition Parse(string template, TokenizerOptions options);
}
