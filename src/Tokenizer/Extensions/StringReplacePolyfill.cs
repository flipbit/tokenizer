#if NETSTANDARD2_0
using System.Text;

namespace Tokens.Extensions;

internal static class StringReplacePolyfill
{
    public static string Replace(this string source, string oldValue, string newValue, StringComparison comparisonType)
    {
        if (string.IsNullOrEmpty(oldValue))
        {
            throw new ArgumentException("String cannot be empty.", nameof(oldValue));
        }

        var result = new StringBuilder();
        var startIndex = 0;
        int index;

        while ((index = source.IndexOf(oldValue, startIndex, comparisonType)) >= 0)
        {
            result.Append(source, startIndex, index - startIndex);
            result.Append(newValue);
            startIndex = index + oldValue.Length;
        }

        result.Append(source, startIndex, source.Length - startIndex);
        return result.ToString();
    }
}
#endif
