using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is an integer
/// </summary>
public sealed class IsIntegerValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return long.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }
}
