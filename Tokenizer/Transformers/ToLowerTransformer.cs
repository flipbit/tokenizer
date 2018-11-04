namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to lower case
    /// </summary>
    public class ToLowerTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return value.ToString().ToLower();
        }
    }
}
