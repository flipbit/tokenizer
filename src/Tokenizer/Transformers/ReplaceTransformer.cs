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
            var valueString = value?.ToString();
            if (string.IsNullOrEmpty(valueString))
            {
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length != 2) throw new TokenizerException($"Replace(from, to): missing arguments processing: {value}");

            transformed = valueString.Replace(args[0], args[1]);

            return true;
        }
    }
}
