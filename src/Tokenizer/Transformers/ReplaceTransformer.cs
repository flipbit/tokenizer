namespace Tokens.Transformers;

/// <summary>
/// Replaces occurrences of a string with another
/// </summary>
public sealed class ReplaceTransformer : ITokenTransformer
{
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 2) throw new ArgumentException($"Replace(from, to): missing arguments processing: {value}");

        transformed = valueString.Replace(args[0], args[1]);

        return true;
    }
}
