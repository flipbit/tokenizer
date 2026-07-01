using System;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value meets a maximum length requirement
/// </summary>
public sealed class MaxLengthValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("You must specify a MaxLength value, e.g. 'MaxLength(255)'");
        }

        try
        {
            var maxLength = Convert.ToInt32(args[0]);

            return (value?.ToString() ?? string.Empty).Length <= maxLength;
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("MaxLength parameter must be an integer", ex);
        }

    }
}
