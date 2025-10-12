using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/>
    /// </summary>
    public class ToDateTimeTransformer : ITokenTransformer
    {
        private static readonly Dictionary<string, string[]> MonthAbbreviations;
        private static readonly object LockHandle;

        static ToDateTimeTransformer()
        {
            MonthAbbreviations = new Dictionary<string, string[]>();
            LockHandle = new object();
        }

        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (TryParseDateTime(value, args, out var result))
            {
                transformed = result;
                return true;
            };

            transformed = value;

            return false;
        }

        public static bool TryParseDateTime(object value, string[] formats, out DateTime result)
        {
            return TryParseDateTime(value, formats, DateTimeStyles.None, out result);
        }

        public static bool TryParseDateTime(object value, string[] formats, DateTimeStyles dateTimeStyles, out DateTime result)
        {
            if (value == null)
            {
                result = default;
                return false;
            }

            var valueString = value
                .ToString()
                .SubstringBeforeNewLine();
            
            if (string.IsNullOrWhiteSpace(valueString))
            {
                result = default;
                return false;
            }

            var cultures = GetCultures(valueString, formats);

            foreach (var culture in cultures)
            {
                if (formats == null || formats.Length == 0 || string.IsNullOrEmpty(formats[0]))
                {
                    if (DateTime.TryParse(valueString, culture, dateTimeStyles, out result))
                    {
                        return true;
                    }
                }
                else
                {
                    foreach (var format in formats)
                    {
                        if (string.IsNullOrWhiteSpace(format)) continue;

                        var valueToFormat = valueString;

                        // Remove day ordinals
                        if (format.Contains(" d ") || 
                            format.Contains(" dd ") || 
                            format.StartsWith("d ") ||
                            format.StartsWith("dd "))
                        {
                            valueToFormat = Regex.Replace(valueToFormat, @"\b(\d+)(?:st|nd|rd|th)\b", "$1"); 
                        }

                        if (DateTime.TryParseExact(valueToFormat, format, culture, dateTimeStyles, out result))
                        {
                            return true;
                        }
                    }
                }
                
            }

            result = default;

            return false;
        }

        private static IEnumerable<CultureInfo> GetCultures(string value, IReadOnlyCollection<string> formats)
        { 
            var cultures = new List<CultureInfo> { CultureInfo.InvariantCulture };

            if (value == null) return cultures;
            if (formats == null) return cultures;
            if (formats.Count < 1) return cultures;

            InitializeCulture("es-US");
            InitializeCulture("es-ES");

            foreach (var format in formats)
            {
               if (string.IsNullOrWhiteSpace(format)) continue;

               if (!format.Contains("MMM")) continue;

               foreach (var key in MonthAbbreviations.Keys)
               {
                   foreach (var abbreviation in MonthAbbreviations[key])
                   {
                       if (value.IndexOf(abbreviation, StringComparison.InvariantCultureIgnoreCase) <= -1) continue;

                       cultures.Add(CultureInfo.GetCultureInfo(key));

                       break;
                   }
               }
            }

            return cultures;
        }

        private static void InitializeCulture(string code)
        {
            if (MonthAbbreviations.ContainsKey(code)) return;

            lock (LockHandle)
            {
                if (MonthAbbreviations.ContainsKey(code)) return;

                try
                {
                    var culture = CultureInfo.GetCultureInfo(code);

                    var list = culture
                        .DateTimeFormat
                        .AbbreviatedMonthNames
                        .Where(m => string.IsNullOrEmpty(m) == false)
                        .ToArray();

                    MonthAbbreviations.Add(code, list);
                }
                catch (CultureNotFoundException)
                {
                }
            }
        }
    }
}
