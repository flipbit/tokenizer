using System.Text.RegularExpressions;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value matches a regular expression pattern
/// </summary>
public sealed class MatchesRegexValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("MatchesRegex(pattern): missing argument — you must specify a regex pattern");
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return Regex.IsMatch(valueString, args[0], RegexOptions.None, TimeSpan.FromSeconds(1));
    }
}
