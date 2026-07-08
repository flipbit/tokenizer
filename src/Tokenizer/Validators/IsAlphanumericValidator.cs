namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value contains only alphanumeric characters
/// </summary>
public sealed class IsAlphanumericValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        foreach (var c in valueString)
        {
            if (!char.IsLetterOrDigit(c)) return false;
        }

        return true;
    }
}
