namespace Tokens.Operators
{
    /// <summary>
    /// Converts the token value to upper case
    /// </summary>
    public class ToUpper : ITokenOperator
    {
        public object Perform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return value.ToString().ToUpper();
        }
    }
}
