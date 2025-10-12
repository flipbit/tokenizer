using System;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown by the <see cref="Tokenizer"/> when an exception occurs
    /// </summary>
    public class TokenizerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TokenizerException(string message) : base(message)
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizerException"/> class.
        /// </summary>
        public TokenizerException(string message, Exception innerException) : base(message, innerException)
        {            
        }
    }
}
