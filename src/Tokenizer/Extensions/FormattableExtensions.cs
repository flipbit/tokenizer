using System.Globalization;

namespace Tokens.Extensions;

internal static class FormattableExtensions
{
    internal static string ToInvariant(this IFormattable value)
        => value.ToString(format: null, CultureInfo.InvariantCulture);

    internal static string ToInvariant(this IFormattable value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);
}
