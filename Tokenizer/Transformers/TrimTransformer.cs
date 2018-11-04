namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value 
    /// </summary>
    public class TrimTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            return value.ToString().Trim();
        }
    }
}
