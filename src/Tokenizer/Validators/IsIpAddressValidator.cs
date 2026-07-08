using System.Net;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is a valid IP address (IPv4 or IPv6)
/// </summary>
public sealed class IsIpAddressValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return IPAddress.TryParse(valueString, out _);
    }
}
