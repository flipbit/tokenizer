#if NETSTANDARD2_0
using Tokens.Extensions;
#endif

namespace Tokens.Transformers;

/// <summary>
/// Replaces occurrences of a string with another
/// </summary>
public sealed class ReplaceTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 2) throw new ArgumentException($"Replace(from, to): missing arguments processing: {value}", nameof(args));

        transformed = valueString.Replace(args[0], args[1], StringComparison.Ordinal);

        return true;
    }
}
