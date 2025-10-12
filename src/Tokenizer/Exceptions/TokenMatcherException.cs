using System;

namespace Tokens.Exceptions
{
    public class TokenMatcherException : TokenizerException
    {
        public TokenMatcherException(string message) : base(message)
        {
        }

        public TokenMatcherException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public Template Template { get; set; }
    }
}
