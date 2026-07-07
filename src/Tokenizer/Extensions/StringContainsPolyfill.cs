#if NETSTANDARD2_0
namespace Tokens.Extensions;

internal static class StringContainsPolyfill
{
    public static bool Contains(this string source, string value, StringComparison comparisonType)
    {
        return source.IndexOf(value, comparisonType) >= 0;
    }
}
#endif
