namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to lower case
    /// </summary>
    public class ToLowerTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            var valueString = value?.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                transformed = string.Empty;
                return true;
            }

            transformed = valueString.ToLower();

            return true;
        }
    }
}
