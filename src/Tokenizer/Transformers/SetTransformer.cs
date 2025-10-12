using System;

namespace Tokens.Transformers
{
    /// <summary>
    /// Sets the token value 
    /// </summary>
    public class SetTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (args == null || args.Length != 1)
            {
                throw new ArgumentException("Set() must specified one argument to set - Set( value)");
            }

            transformed = args[0].Trim();

            return true;
        }
    }
}
