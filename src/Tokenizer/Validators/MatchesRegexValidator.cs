using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value matches a regular expression pattern.
/// Patterns are cached for performance. The cache is bounded to <see cref="MaxCacheSize"/>
/// entries; when full, new patterns are evaluated without caching.
/// </summary>
public sealed class MatchesRegexValidator : IOptionsAwareValidator
{
    private const int MaxCacheSize = 1024;
    private static readonly ConcurrentDictionary<(string Pattern, TimeSpan Timeout), Regex> RegexCache = new();

    /// <summary>
    /// Determines whether the specified token is valid.
    /// Uses the default 1-second regex timeout.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        return IsValid(value, args, new TokenizerOptions());
    }

    /// <summary>
    /// Determines whether the specified token is valid, using the timeout from options.
    /// </summary>
    public bool IsValid(object value, string[] args, TokenizerOptions options)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("MatchesRegex(pattern): missing argument — you must specify a regex pattern", nameof(args));
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        var timeout = options.MaxRegexTimeout;
        var cacheKey = (args[0], timeout);

        if (!RegexCache.TryGetValue(cacheKey, out var regex))
        {
            regex = new Regex(args[0], RegexOptions.None, timeout);

            if (RegexCache.Count < MaxCacheSize)
            {
                RegexCache.TryAdd(cacheKey, regex);
            }
        }

        try
        {
            return regex.IsMatch(valueString);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
