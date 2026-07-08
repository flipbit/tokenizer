using Tokens.Temporal;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a parseable date/time string.
/// Time is optional (defaults to midnight).
/// </summary>
public sealed class IsDateTimeValidator : IOptionsAwareValidator
{
    /// <inheritdoc />
    public bool IsValid(object value, params string[] args)
    {
        return IsValid(value, args, new TokenizerOptions());
    }

    /// <inheritdoc />
    public bool IsValid(object value, string[] args, TokenizerOptions options)
    {
        if (value == null) return false;

        var valueString = value.ToString();
        if (string.IsNullOrEmpty(valueString)) return false;

        return TemporalParser.TryParse(valueString, args, options, out _);
    }
}
