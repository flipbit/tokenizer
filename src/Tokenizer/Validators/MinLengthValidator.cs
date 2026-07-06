namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value meets a minimum length requirement
/// </summary>
public sealed class MinLengthValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("You must specify a MinLength value, e.g. 'MinLength(50)'", nameof(args));
        }

        try
        {
            var minLength = Convert.ToInt32(args[0]);

            return (value?.ToString() ?? string.Empty).Length >= minLength;
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("MinLength parameter must be an integer", nameof(args), ex);
        }

    }
}
