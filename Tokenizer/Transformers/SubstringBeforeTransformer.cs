using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value after the first occurence of the given string 
    /// </summary>
    public class SubstringBeforeTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) return string.Empty;

            if (args == null || args.Length == 0) throw new TokenizerException($"SubstringBefore(): missing argument processing: {value}");

            return value.ToString().SubstringBeforeString(args[0]);
        }
    }
}
