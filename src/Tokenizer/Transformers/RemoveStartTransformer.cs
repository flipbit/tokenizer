using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Removes occurrences of a string from the start of a token value
/// </summary>
public sealed class RemoveStartTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new ArgumentException($"RemoveStart(value): missing arguments processing: {value}", nameof(args));

        transformed = valueString.StartsWith(args[0], StringComparison.Ordinal)
            ? valueString.SubstringAfterString(args[0])
            : value;

        return true;
    }
}
