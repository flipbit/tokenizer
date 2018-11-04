using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value after the first occurence of the given string 
    /// </summary>
    public class SubstringAfterTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) return string.Empty;

            if (args == null || args.Length == 0) throw new TokenizerException($"SubstringAfter(): missing argument processing: {value}");

            return value.ToString().SubstringAfterString(args[0]);
        }
    }
}
