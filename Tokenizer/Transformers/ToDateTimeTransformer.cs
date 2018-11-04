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

            DateTime result;

            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                DateTime.TryParse(value.ToString(), out result);
            }
            else
            {
                DateTime.TryParseExact(value.ToString(), args[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }

            return result;
        }
    }
}
