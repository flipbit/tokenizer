namespace Tokens.Compilation.Parsing
{
    internal enum TemplateDefinitionParserState
    {
        AtStart,
        InFrontMatter,
        InFrontMatterOption,
        InFrontMatterOptionValue,
        InFrontMatterComment,
        InPreamble,
        InTokenName,
        InDecorator,
        InDecoratorArgument,
        InDecoratorArgumentSingleQuotes,
        InDecoratorArgumentDoubleQuotes,
        InDecoratorArgumentRunOff,
        InTokenValue,
        InTokenValueSingleQuotes,
        InTokenValueDoubleQuotes,
        InTokenValueRunOff
    }
}
