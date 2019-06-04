using System;

namespace Tokens.Transformers
{
    /// <summary>
    /// Sets the token value 
    /// </summary>
    public class SetTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (args == null || args.Length != 1)
            {
                throw new ArgumentException("Set() must specified one argument to set - Set( value)");
            }

            return args[0].Trim();
        }
    }
}
