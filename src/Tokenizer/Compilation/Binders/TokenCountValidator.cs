using Tokens.Exceptions;

namespace Tokens.Compilation.Binders;

internal static class TokenCountValidator
{
    public static void Validate(Template template, TokenizerOptions options)
    {
        if (options.MaxTokenCount > 0 && template.Tokens.Count > options.MaxTokenCount)
        {
            throw new ParsingException(
                $"Template contains {template.Tokens.Count} tokens, exceeding maximum of {options.MaxTokenCount:N0}. " +
                "Increase TokenizerOptions.MaxTokenCount to allow more tokens.",
                new Enumerators.FileLocation());
        }
    }
}
