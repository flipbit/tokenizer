namespace Tokens.Transformers;

/// <summary>
/// Sets the token value
/// </summary>
public sealed class SetTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (args == null || args.Length != 1)
        {
            throw new ArgumentException("Set() must specify one argument to set - Set(value)", nameof(args));
        }

        transformed = args[0].Trim();

        return true;
    }
}
