using System.Collections.Generic;
using System.Linq;
using Tokens.Exceptions;

namespace Tokens
{
    /// <summary>
    /// Holds the result of attempting to parse an input string against a
    /// <see cref="Template"/>.
    /// </summary>
    public class TokenizeResult : TokenizeResultBase 
    {
        /// <summary>
        ///  Creates a new instance of the <see cref="TokenizeResult"/> class.
        /// </summary>
        public TokenizeResult(Template template) : base(template)
        {
        }

        /// <summary>
        /// A dictionary of values extracted from the input string. 
        /// </summary>
        public IList<Match> Matches => Tokens.Matches;

        public object First(string key)
        {
            if (Matches.Any(m => m.Token.Name == key) == false)
            {
                throw new TokenizerException($"Token '{key}' was not found in the input text.");
            }

            return Matches.First(m => m.Token.Name == key).Value;
        }

        public T First<T>(string key)
        {
            if (Matches.Any(m => m.Token.Name == key) == false)
            {
                throw new TokenizerException($"Token '{key}' was not found in the input text.");
            }

            return (T) Matches.First(m => m.Token.Name == key).Value;
        }

        public object FirstOrDefault(string key)
        {
            if (Matches.Any(m => m.Token?.Name == key))
            {
                return Matches.First(m => m.Token.Name == key).Value;
            }

            return null;
        }

        public T FirstOrDefault<T>(string key)
        {
            if (Matches.Any(m => m.Token?.Name == key))
            {
                return (T) Matches.First(m => m.Token.Name == key).Value;
            }

            return default(T);
        }

        public IList<object> All(string key)
        {
            return Matches
                .Where(m => m.Token.Name == key)
                .Select(m => m.Value)
                .ToList();
        }

        public bool Contains(string key)
        {
            return Matches.Any(m => m.Token.Name == key);
        }
    }    
    
    /// <summary>
    /// Holds the result of attempting to parse an input string against a
    /// <see cref="Template"/> to generate an object of type <see cref="T"/>.
    /// </summary>
    public class TokenizeResult<T> : TokenizeResultBase where T : class, new()
    {
        /// <summary>
        ///  Creates a new instance of the <see cref="TokenizeResult{T}"/> class.
        /// </summary>
        public TokenizeResult(Template template) : base(template)
        {
            Value = new T();
        }

        /// <summary>
        /// An instance of <see cref="T"/> populated with data from the input string. 
        /// </summary>
        public T Value { get; set; }
    }
}
