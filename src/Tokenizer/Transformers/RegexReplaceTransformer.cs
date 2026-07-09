using System.Text.RegularExpressions;

namespace Tokens.Transformers;

/// <summary>
/// Replaces occurrences matching a regular expression pattern.
/// </summary>
public sealed class RegexReplaceTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <summary>
    /// Transforms the value using options-aware regex replacement.
    /// </summary>
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length < 2)
        {
            throw new ArgumentException($"RegexReplace(pattern, replacement): missing arguments processing: {value}", nameof(args));
        }

        try
        {
            transformed = Regex.Replace(valueString, args[0], args[1], RegexOptions.None, options.MaxRegexTimeout);
            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            transformed = value;
            return false;
        }
    }
}
