using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Removes occurrences of a string from the end of a token value
/// </summary>
public sealed class RemoveEndTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new ArgumentException($"RemoveEnd(value): missing arguments processing: {value}", nameof(args));

        transformed = valueString.EndsWith(args[0], StringComparison.Ordinal)
            ? valueString.SubstringBeforeLastString(args[0])
            : value;

        return true;
    }
}
