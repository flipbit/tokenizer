using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Tokens.Extensions
{
    /// <summary>
    /// String extension class
    /// </summary>
    public static class StringExtensions
    {
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
                if (value.Contains(match))
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
                    if (value.Contains(match))
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
                    if (value.Contains(match))
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
                if (value.Contains(match))
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
                if (value.Contains(match))
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
                if (value.Contains(match))
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
                    if (value.Contains(match))
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
                return new string[0];
            }

            return Regex.Split(value, "\r\n|\r|\n");
        }

        public static bool IsOnlySpaces(this string value)
        {
            if (string.IsNullOrEmpty(value) == false)
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

        public static string TrimLeadingSpaces(this string value)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(value) == false)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var character = value.Substring(i, 1);

                    if (character ==" ") continue;

                    return value.Substring(i);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the given string is null or white space.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string value)
        {
#if DOTNET35
            var result = string.IsNullOrEmpty(value);

            if (!result)
            {
                result = value.Trim() == string.Empty;
            }

            return result;
#else
            return string.IsNullOrWhiteSpace(value);
#endif
        }

    }
}