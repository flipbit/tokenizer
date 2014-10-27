using System;
using System.Globalization;

namespace Tokens.Operators
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/>
    /// </summary>
    public class ToDateTimeOperator : ITokenOperator
    {
        /// <summary>
        /// Performs the operation on the specified value.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Perform(Function function, Token token, string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            DateTime result;

            if (function.Parameters.Count == 0 || string.IsNullOrEmpty(function.Parameters[0]))
            {
                DateTime.TryParse(value, out result);
            }
            else
            {
                DateTime.TryParseExact(value, function.Parameters[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }

            return result;
        }
    }
}
