using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Validates that a compiled template does not exceed the configured maximum token count.
/// </summary>
internal static class TokenCountValidator
{
    public static void Validate(Template template, TokenizerOptions options)
    {
        if (options.MaxTokenCount > 0 && template.Tokens.Count > options.MaxTokenCount)
        {
            throw new ParsingException(
                $"Template contains {template.Tokens.Count.ToInvariant()} tokens, exceeding maximum of {options.MaxTokenCount.ToInvariant("N0")}. " +
                "Increase TokenizerOptions.MaxTokenCount to allow more tokens.",
                new Enumerators.FileLocation());
        }
    }
}
