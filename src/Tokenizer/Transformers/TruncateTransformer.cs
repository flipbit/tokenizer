using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Truncates the token value to a maximum length
/// </summary>
public sealed class TruncateTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length == 0)
        {
            throw new ArgumentException($"Truncate(maxLength): missing argument processing: {value}", nameof(args));
        }

        try
        {
            var maxLength = Convert.ToInt32(args[0], CultureInfo.InvariantCulture);

            transformed = valueString.Length <= maxLength
                ? valueString
                : valueString.Substring(0, maxLength);

            return true;
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Truncate parameter must be an integer", nameof(args), ex);
        }
    }
}
