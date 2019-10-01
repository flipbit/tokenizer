using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Removes occurrences of a string from the end of a token value
    /// </summary>
    public class RemoveEndTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) 
            { 
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length != 1) throw new TokenizerException($"RemoveEnd(value): missing arguments processing: {value}");

            var valueString = value.ToString();
            if (valueString.EndsWith(args[0]))
            {
                transformed = valueString.SubstringBeforeLastString(args[0]);
            }
            else
            {
                transformed = value;
            }

            return true;
        }
    }
}
