using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Trims the token value after the first occurence of the given string
/// </summary>
public sealed class SubstringAfterTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length == 0) throw new ArgumentException($"SubstringAfter(): missing argument processing: {value}", nameof(args));

        transformed = valueString.SubstringAfterString(args[0]);

        return true;
    }
}
