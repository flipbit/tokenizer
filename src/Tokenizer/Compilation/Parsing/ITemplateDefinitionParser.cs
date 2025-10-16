using Tokens.Compilation.Definitions;

namespace Tokens.Compilation.Parsing;

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