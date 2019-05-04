using System;
using System.Globalization;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/>
    /// </summary>
    public class ToDateTimeTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return ToDateTime(value, args, out var result) ? result : value;
        }

        public static bool ToDateTime(object value, string[] formats, out DateTime result)
        {
            if (formats == null || formats.Length == 0 || string.IsNullOrEmpty(formats[0]))
            {
                if (DateTime.TryParse(value.ToString(), out result))
                {
                    return true;
                }
            }
            else
            {
                
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(value.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        return true;
                    }
                }
            }

            result = default(DateTime);

            return false;
        }
    }
}
