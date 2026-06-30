using System;

namespace Tokens.Exceptions
{
    /// <summary>
    /// Thrown when a value can't be converted into it's destination type
    /// </summary>
    public class TypeConversionException : TokenizerException
    {
        public TypeConversionException(string message, object value, Type targetType) : base(message)
        {
            Value = value;
            TargetType = targetType;
        }

        public TypeConversionException(string message, object value, Type targetType, Exception innerException) : base(message, innerException)
        {
            Value = value;
            TargetType = targetType;
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
