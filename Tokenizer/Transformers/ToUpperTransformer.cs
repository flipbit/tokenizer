namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to upper case
    /// </summary>
    public class ToUpperTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return value.ToString().ToUpper();
        }
    }
}
