namespace Tokens.Validators;

/// <summary>
/// A validator that receives <see cref="TokenizerOptions"/> for context-dependent
/// operations such as culture-aware date/time validation.
/// </summary>
public interface IOptionsAwareValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token value is valid using the specified options for context.
    /// </summary>
    public bool IsValid(object value, string[] args, TokenizerOptions options);
}
