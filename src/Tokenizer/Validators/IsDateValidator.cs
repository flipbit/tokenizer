#if NET6_0_OR_GREATER
using Tokens.Temporal;

namespace Tokens.Validators;

/// <summary>
/// Validates that the token value is a date-only string.
/// Fails if a time component is present.
/// </summary>
public sealed class IsDateValidator : IOptionsAwareValidator
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

        if (!TemporalParser.TryParse(valueString, args, options, out var dto))
            return false;

        // Reject if time component is non-midnight
        return dto.TimeOfDay == TimeSpan.Zero;
    }
}
#endif
