using System;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown when a value can't be converted into it's destination type
    /// </summary>
    public class TypeConversionException : TokenizerException
    {
        public TypeConversionException(string message) : base(message)
        {
        }

        public TypeConversionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// The target type to be converted to
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// The value being converted
        /// </summary>
        public object Value { get; set; }
    }
}
