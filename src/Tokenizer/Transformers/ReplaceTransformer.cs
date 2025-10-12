using Tokens.Exceptions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Replaces occurrences of a string with another
    /// </summary>
    public class ReplaceTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) 
            { 
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length != 2) throw new TokenizerException($"Replace(from, to): missing arguments processing: {value}");

            transformed = value.ToString().Replace(args[0], args[1]);

            return true;
        }
    }
}
