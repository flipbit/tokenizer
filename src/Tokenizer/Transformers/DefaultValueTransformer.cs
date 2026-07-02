namespace Tokens.Transformers;

/// <summary>
/// Returns a fallback value when the token value is null or empty
/// </summary>
public sealed class DefaultValueTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("DefaultValue(fallback): missing argument — you must specify a fallback value");
        }

        var valueString = value?.ToString();

        if (string.IsNullOrEmpty(valueString))
        {
            transformed = args[0];
            return true;
        }

        transformed = value!;
        return true;
    }
}
