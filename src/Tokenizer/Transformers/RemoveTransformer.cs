#if NETSTANDARD2_0
using Tokens.Extensions;
#endif

namespace Tokens.Transformers;

/// <summary>
/// Removes occurrences of a string
/// </summary>
public sealed class RemoveTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new ArgumentException($"Remove(value): missing arguments processing: {value}", nameof(args));

        transformed = valueString.Replace(args[0], string.Empty, StringComparison.Ordinal);

        return true;
    }
}
