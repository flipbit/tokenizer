namespace Tokens.Exceptions;

/// <summary>
/// Thrown when a value can't be converted into it's destination type
/// </summary>
public class TypeConversionException : TokenizerException
{
    /// <summary>
    /// Initializes a new instance with a message, the value that failed conversion, and the target type.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="value">The value that could not be converted.</param>
    /// <param name="targetType">The type the value was being converted to.</param>
    public TypeConversionException(string message, object value, Type targetType) : base(message)
    {
        Value = value;
        TargetType = targetType;
    }

    /// <summary>
    /// Initializes a new instance with a message, the value that failed conversion, the target type, and an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="value">The value that could not be converted.</param>
    /// <param name="targetType">The type the value was being converted to.</param>
    /// <param name="innerException">The exception that caused the conversion to fail.</param>
    public TypeConversionException(string message, object value, Type targetType, Exception innerException) : base(message, innerException)
    {
        Value = value;
        TargetType = targetType;
    }

    /// <summary>
    /// The target type to be converted to
    /// </summary>
    public Type TargetType { get; init; }

    /// <summary>
    /// The value being converted
    /// </summary>
    public object Value { get; init; }
}
