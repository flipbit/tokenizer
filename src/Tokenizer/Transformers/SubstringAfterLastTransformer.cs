using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Trims the token value after the last occurrence of the given string
/// </summary>
public sealed class SubstringAfterLastTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length == 0) throw new ArgumentException($"SubstringAfterLast(): missing argument processing: {value}");

        transformed = valueString.SubstringAfterLastString(args[0]);

        return true;
    }
}
