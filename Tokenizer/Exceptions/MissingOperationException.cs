using System;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown when an operation can't be mapped from a pattern
    /// </summary>
    public class MissingOperationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingOperationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingOperationException(string message) : base(message)
        {            
        }
    }
}
