namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value 
    /// </summary>
    public class TrimTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            var valueString = value?.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                transformed = string.Empty;
                return true;
            }

            transformed = valueString.Trim();

            return true;
        }
    }
}
