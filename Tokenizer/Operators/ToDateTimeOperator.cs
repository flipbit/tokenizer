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
        public object Perform(Function function, Token token, object value)
        {
            if (value == null) return string.Empty;

            DateTime result;

            if (function.Parameters.Count == 0 || string.IsNullOrEmpty(function.Parameters[0]))
            {
                DateTime.TryParse(value.ToString(), out result);
            }
            else
            {
                DateTime.TryParseExact(value.ToString(), function.Parameters[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }

            return result;
        }
    }
}
