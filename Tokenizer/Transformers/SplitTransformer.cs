using System;
using Tokens.Exceptions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Removes occurrences of a string from then end of a token value
    /// </summary>
    public class SplitTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) 
            { 
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length != 1) throw new TokenizerException($"Split(value): missing arguments processing: {value}");

            var valueArray = value.ToString().Split(new[] { args[0] }, StringSplitOptions.RemoveEmptyEntries);
            if (valueArray.Length > 1)
            {
                transformed = valueArray;
            }
            else
            {
                transformed = value;
            }

            return true;
        }
    }
}
