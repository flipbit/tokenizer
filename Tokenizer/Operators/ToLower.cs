namespace Tokens.Operators
{
    /// <summary>
    /// Converts the token value to lower case
    /// </summary>
    public class ToLower : ITokenOperator
    {
        public object Perform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return value.ToString().ToLower();
        }
    }
}
