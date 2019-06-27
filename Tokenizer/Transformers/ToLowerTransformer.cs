namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to lower case
    /// </summary>
    public class ToLowerTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null) 
            {
                transformed = string.Empty;
                return true;
            }

            transformed = value.ToString().ToLower();

            return true;
        }
    }
}
