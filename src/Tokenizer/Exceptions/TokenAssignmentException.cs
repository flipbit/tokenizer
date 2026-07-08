namespace Tokens.Exceptions;

/// <summary>
/// Thrown when a matched token value cannot be assigned to the target property.
/// </summary>
public class TokenAssignmentException : TokenizerException
{
    /// <summary>
    /// The token whose value could not be assigned to the target property.
    /// </summary>
    public Token Token { get; }

    /// <summary>
    /// Initializes a new instance with the token that failed and a descriptive message.
    /// </summary>
    /// <param name="token">The token that could not be assigned.</param>
    /// <param name="message">The error message.</param>
    public TokenAssignmentException(Token token, string message) : base(message)
    {
        Token = token;
    }

    /// <summary>
    /// Initializes a new instance wrapping an inner exception that prevented the assignment.
    /// </summary>
    /// <param name="token">The token that could not be assigned.</param>
    /// <param name="innerException">The exception that caused the assignment to fail.</param>
    public TokenAssignmentException(Token token, Exception innerException) : base($"Unable to assign: {token.Name}", innerException)
    {
        Token = token;
    }
}
