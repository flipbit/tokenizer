using Tokens.Exceptions;

namespace Tokens.Transformers;

/// <summary>
/// Removes occurrences of a string
/// </summary>
public sealed class RemoveTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new TokenizerException($"Remove(value): missing arguments processing: {value}");

        transformed = valueString.Replace(args[0], string.Empty);

        return true;
    }
}
