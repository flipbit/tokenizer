using System.Collections.Generic;

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
            Values = new Dictionary<string, object>();
        }

        /// <summary>
        /// A dictionary of values extracted from the input string. 
        /// </summary>
        public IDictionary<string, object> Values { get; set; }
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
