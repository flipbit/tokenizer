using System;
using System.Globalization;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/>
    /// </summary>
    public class ToDateTimeTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            return TryParseDateTime(value, args, out var result) ? result : value;
        }

        public static bool TryParseDateTime(object value, string[] formats, out DateTime result)
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

            if (formats == null || formats.Length == 0 || string.IsNullOrEmpty(formats[0]))
            {
                if (DateTime.TryParse(valueString, out result))
                {
                    return true;
                }
            }
            else
            {
                
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(valueString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        return true;
                    }
                }
            }

            result = default;

            return false;
        }
    }
}
