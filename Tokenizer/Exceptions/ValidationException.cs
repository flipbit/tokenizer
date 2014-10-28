using System;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown when an validation can't be mapped from a pattern
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ValidationException(string message) : base(message)
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public ValidationException(string message, Exception exception) : base(message, exception)
        {            
        }
    }
}
