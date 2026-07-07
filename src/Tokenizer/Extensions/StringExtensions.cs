using System.Text;
using System.Text.RegularExpressions;

namespace Tokens.Extensions;

/// <summary>
/// String extension class
/// </summary>
public static class StringExtensions
{
    private static readonly Regex NewLineSplitRegex = new(@"\r\n|\r|\n", RegexOptions.Compiled, TimeSpan.FromMilliseconds(-1));
    /// <summary>
    /// Gets the substring after the first matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="match">The match.</param>
    /// <returns></returns>
    public static string SubstringAfterString(this string value, string match)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(match))
        {
            if (value.Contains(match, StringComparison.Ordinal))
            {
                result = value.Substring(value.IndexOf(match, StringComparison.Ordinal) + match.Length);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the substring after the first matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="matches">The matches.  Only the first match is used.</param>
    /// <returns></returns>
    public static string SubstringAfterAnyString(this string value, params string[] matches)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && matches != null)
        {
            foreach (var match in matches)
            {
                if (value.Contains(match, StringComparison.Ordinal))
                {
                    result = value.SubstringAfterString(match);

                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the substring after the first matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="matches">The matches.  Only the first match is used.</param>
    /// <returns></returns>
    public static string SubstringAfterLastAnyString(this string value, params string[] matches)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && matches != null)
        {
            foreach (var match in matches)
            {
                if (value.Contains(match, StringComparison.Ordinal))
                {
                    result = value.SubstringAfterLastString(match);

                    break;
                }
            }
        }

        return result;
    }


    /// <summary>
    /// Gets the substring after the last matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="match">The match.</param>
    /// <returns></returns>
    public static string SubstringAfterLastString(this string value, string match)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(match))
        {
            if (value.Contains(match, StringComparison.Ordinal))
            {
                result = value.Substring(value.LastIndexOf(match, StringComparison.Ordinal) + match.Length);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the substring before the first matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="match">The match.</param>
    /// <returns></returns>
    public static string SubstringBeforeString(this string value, string match)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(match))
        {
            if (value.Contains(match, StringComparison.Ordinal))
            {
                result = value.Substring(0, value.IndexOf(match, StringComparison.Ordinal));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the substring before the last matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="match">The match.</param>
    /// <returns></returns>
    public static string SubstringBeforeLastString(this string value, string match)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(match))
        {
            if (value.Contains(match, StringComparison.Ordinal))
            {
                result = value.Substring(0, value.LastIndexOf(match, StringComparison.Ordinal));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the substring after the first matching string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="matches">The matches.  Only the first match is used.</param>
    /// <returns></returns>
    public static string SubstringBeforeAnyString(this string value, params string[] matches)
    {
        var result = value;

        if (!string.IsNullOrEmpty(value) && matches != null)
        {
            foreach (var match in matches)
            {
                if (value.Contains(match, StringComparison.Ordinal))
                {
                    result = value.SubstringBeforeString(match);

                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns an enumerable collection of all the lines in the given string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static IEnumerable<string> ToLines(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Array.Empty<string>();
        }

        return NewLineSplitRegex.Split(value);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the string is non-empty and consists entirely of space characters.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> if the string contains only spaces; otherwise <see langword="false"/>.</returns>
    public static bool IsOnlySpaces(this string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            foreach (var character in value.ToCharArray())
            {
                if (character != ' ')
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all leading space characters from the string, leaving all other characters intact.
    /// </summary>
    /// <param name="value">The value to trim.</param>
    /// <returns>The string with leading spaces removed.</returns>
    public static string TrimLeadingSpaces(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != ' ') return value.Substring(i);
        }

        return string.Empty;
    }

    /// <summary>
    /// Determines whether the given string is null or white space.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Keeps the specified characters in the given value, removed the rest.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="keepTheseCharacters">The keep these characters.</param>
    /// <returns></returns>
    public static string Keep(this string value, string keepTheseCharacters)
    {
        var result = new StringBuilder();

        if (!string.IsNullOrEmpty(value) &&
            !string.IsNullOrEmpty(keepTheseCharacters))
        {
            var allowed = new HashSet<char>(keepTheseCharacters);

            foreach (var character in value)
            {
                if (!allowed.Contains(character)) continue;

                result.Append(character);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the substring before the first newline.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    public static string SubstringBeforeNewLine(this string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
            // Only run conversion up to a new line character
            var newlineIndex = value.IndexOf('\n');
            if (newlineIndex > -1)
            {
                value = value.Substring(0, newlineIndex);
            }

            // handle Windows newlines too
            newlineIndex = value.IndexOf('\r');
            if (newlineIndex > -1)
            {
                value = value.Substring(0, newlineIndex);
            }
#pragma warning restore MA0001
        }

        return value;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the string ends with a Unix (<c>\n</c>) or Windows (<c>\r\n</c>) newline.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> if the string ends with a newline sequence; otherwise <see langword="false"/>.</returns>
    public static bool EndsWithNewLine(this string value)
    {
        return !string.IsNullOrEmpty(value) && value[value.Length - 1] == '\n';
    }

    /// <summary>
    /// Removes a single trailing newline (<c>\n</c> or <c>\r\n</c>) from the string, if present.
    /// </summary>
    /// <param name="value">The value to trim.</param>
    /// <returns>The string with the trailing newline removed, or the original string if none was present.</returns>
    public static string TrimTrailingNewLine(this string value)
    {
        if (!value.EndsWithNewLine()) return value;

        // EndsWithNewLine confirmed value ends with '\n'.
        // Check for Windows-style '\r\n' by inspecting the penultimate character.
        if (value.Length >= 2 && value[value.Length - 2] == '\r')
        {
            return value.Substring(0, value.Length - 2);
        }

        return value.Substring(0, value.Length - 1);
    }

    /// <summary>
    /// Converts the value to a compact, log-safe string with control characters escaped
    /// and the output truncated to 65 characters.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A truncated string with <c>\r</c>, <c>\n</c>, and <c>\t</c> shown as escape sequences.</returns>
    public static string ToLogInfoString(this object value)
    {
        if (value == null) return string.Empty;

        var sb = new StringBuilder();

        string str = value.ToString() ?? string.Empty;

        for (int i = 0; i < str.Length; i++)
        {
            char @char = str[i];

            switch (@char)
            {
                case '\r':
                    sb.Append("\\r");
                    break;

                case '\n':
                    sb.Append("\\n");
                    break;

                case '\t':
                    sb.Append("\\t");
                    break;

                default:
                    sb.Append(@char);
                    break;
            }

            if (sb.Length > 65)
            {
                if (i != str.Length - 1)
                {
                    sb.Append("...");
                }

                break;
            }
        }

        return sb.ToString();
    }

}
