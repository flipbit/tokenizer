namespace Tokens.Transformers;

/// <summary>
/// Splits a token value on a specified delimiter
/// </summary>
public sealed class SplitTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new ArgumentException($"Split(value): missing arguments processing: {value}", nameof(args));

        var valueArray = valueString.Split(new[] { args[0] }, StringSplitOptions.RemoveEmptyEntries);
        transformed = valueArray.Length > 1 ? valueArray : value;

        return true;
    }
}
