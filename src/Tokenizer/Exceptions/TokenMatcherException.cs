namespace Tokens.Exceptions;

/// <summary>
/// Thrown when a fatal error occurs while matching a template against input text.
/// </summary>
public class TokenMatcherException : TokenizerException
{
    /// <summary>
    /// Initializes a new instance with the given error message and the template that triggered it.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="template">The template that was being matched when the error occurred.</param>
    public TokenMatcherException(string message, Template template) : base(message)
    {
        Template = template;
    }

    /// <summary>
    /// Initializes a new instance with the given error message, template, and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="template">The template that was being matched when the error occurred.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public TokenMatcherException(string message, Template template, Exception innerException) : base(message, innerException)
    {
        Template = template;
    }

    /// <summary>
    /// The template that was being processed when the error occurred.
    /// </summary>
    public Template Template { get; init; }
}
