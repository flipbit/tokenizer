using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is within a numeric range (inclusive)
/// </summary>
public sealed class IsInRangeValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length < 2)
        {
            throw new ArgumentException("IsInRange(min, max): you must specify both min and max values");
        }

        if (!decimal.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var min))
        {
            throw new ArgumentException($"IsInRange(min, max): min value '{args[0]}' is not a valid number");
        }

        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var max))
        {
            throw new ArgumentException($"IsInRange(min, max): max value '{args[1]}' is not a valid number");
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        if (!decimal.TryParse(valueString, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        return numericValue >= min && numericValue <= max;
    }
}
