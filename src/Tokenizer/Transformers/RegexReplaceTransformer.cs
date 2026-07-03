using System.Text.RegularExpressions;

namespace Tokens.Transformers;

/// <summary>
/// Replaces occurrences matching a regular expression pattern
/// </summary>
public sealed class RegexReplaceTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length < 2)
        {
            throw new ArgumentException($"RegexReplace(pattern, replacement): missing arguments processing: {value}");
        }

        transformed = Regex.Replace(valueString, args[0], args[1], RegexOptions.None, TimeSpan.FromSeconds(1));

        return true;
    }
}
