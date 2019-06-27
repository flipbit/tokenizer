namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value 
    /// </summary>
    public class TrimTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null) 
            {
                transformed = string.Empty;
                return true;
            }

            transformed  = value.ToString().Trim();

            return true;
        }
    }
}
