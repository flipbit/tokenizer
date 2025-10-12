namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to upper case
    /// </summary>
    public class ToUpperTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null) 
            {
                transformed = string.Empty;
            }
            else
            {
                transformed = value.ToString().ToUpper();
            }

            return true;
        }
    }
}
