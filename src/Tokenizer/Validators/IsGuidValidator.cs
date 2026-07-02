namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is a valid GUID
/// </summary>
public sealed class IsGuidValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return Guid.TryParse(valueString, out _);
    }
}
