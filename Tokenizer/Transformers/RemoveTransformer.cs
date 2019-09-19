using Tokens.Exceptions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Removes occurrences of a string
    /// </summary>
    public class RemoveTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) 
            { 
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length != 1) throw new TokenizerException($"Remove(value): missing arguments processing: {value}");

            transformed = value.ToString().Replace(args[0], string.Empty);

            return true;
        }
    }
}
