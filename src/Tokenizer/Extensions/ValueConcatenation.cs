namespace Tokens.Extensions;

/// <summary>
/// Utility methods for concatenating token values.
/// </summary>
internal static class ValueConcatenation
{
    /// <summary>
    /// Returns <see langword="true"/> if the existing and new values can be concatenated.
    /// Currently only string-to-string concatenation is supported.
    /// </summary>
    internal static bool CanConcatenate(object? existingValue, object newValue)
    {
        if (existingValue is string && newValue is string)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Concatenates two values using the specified joining string.
    /// The literal <c>&lt;CR&gt;</c> in the joining string is replaced with <see cref="Environment.NewLine"/>.
    /// Returns the existing value unchanged if the values are not both strings.
    /// </summary>
    internal static object? Concatenate(object? existingValue, object newValue, string? concatenationString)
    {
        if (existingValue is string && newValue is string)
        {
            var concatStringValue = (concatenationString ?? string.Empty).Replace("<CR>", Environment.NewLine, StringComparison.Ordinal);

            return $"{existingValue}{concatStringValue}{newValue}";
        }

        return existingValue;
    }
}
