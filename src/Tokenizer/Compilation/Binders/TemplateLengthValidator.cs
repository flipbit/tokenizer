using Tokens.Exceptions;

namespace Tokens.Compilation.Binders;

internal static class TemplateLengthValidator
{
    public static void Validate(string content, TokenizerOptions options)
    {
        if (options.MaxTemplateLength > 0 && content.Length > options.MaxTemplateLength)
        {
            throw new ParsingException(
                $"Template length {content.Length:N0} exceeds maximum allowed length of {options.MaxTemplateLength:N0}. " +
                "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.",
                new Enumerators.FileLocation());
        }
    }
}
