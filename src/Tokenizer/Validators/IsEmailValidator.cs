using System.Net.Mail;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is an email address 
/// </summary>
public sealed class IsEmailValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;
        if (valueString.Contains("@-", StringComparison.Ordinal)) return false;
        if (valueString.Trim().Contains(" ", StringComparison.Ordinal)) return false;

        try
        {
            var address = new MailAddress(valueString);

            return string.Equals(address.Address, valueString, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
