#if NET6_0_OR_GREATER
using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a time-only string.
/// Fails if a date component is present.
/// </summary>
public sealed class IsTimeValidator : IOptionsAwareValidator
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

        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
        {
            foreach (var format in args)
            {
                if (TimeOnly.TryParseExact(valueString, format, culture, DateTimeStyles.None, out _))
                    return true;
            }
            return false;
        }

        return TimeOnly.TryParse(valueString, culture, DateTimeStyles.None, out _);
    }
}
#endif
