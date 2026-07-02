namespace Tokens.Exceptions;

/// <summary>
/// Thrown when a fatal error occurs while matching a template against input text.
/// </summary>
public class TokenMatcherException : TokenizerException
{
    public TokenMatcherException(string message, Template template) : base(message)
    {
        Template = template;
    }

    public TokenMatcherException(string message, Template template, Exception innerException) : base(message, innerException)
    {
        Template = template;
    }

    public Template Template { get; init; }
}
