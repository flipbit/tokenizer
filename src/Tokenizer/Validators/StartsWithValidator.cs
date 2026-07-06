namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value starts with a given string
/// </summary>
public sealed class StartsWithValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        if (args == null || args.Length == 0) throw new ArgumentException($"StartsWith(): missing argument processing: {value}", nameof(args));

        return valueString.StartsWith(args[0]);
    }
}
