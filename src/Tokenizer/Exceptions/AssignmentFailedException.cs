namespace Tokens.Exceptions;

/// <summary>
/// Thrown when one or more errors occur while assigning matched token values
/// to the target object's properties.
/// </summary>
public sealed class AssignmentFailedException : TokenizerException
{
    /// <summary>
    /// Initializes a new instance with a message and the individual errors that occurred.
    /// </summary>
    /// <param name="message">A summary message describing the failure.</param>
    /// <param name="errors">The individual exceptions encountered during assignment.</param>
    public AssignmentFailedException(string message, IReadOnlyList<Exception> errors)
        : base(message)
    {
        Errors = errors;
    }

    /// <summary>
    /// The individual exceptions that occurred during assignment.
    /// </summary>
    public IReadOnlyList<Exception> Errors { get; }

    /// <summary>
    /// The partially populated target object, if assignment was attempted.
    /// Use this to retrieve successfully assigned values when handling the exception.
    /// </summary>
    public object? PartialResult { get; internal set; }
}
