namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValidatorRejection issues by explaining why the value
/// failed validation for known validator types.
/// </summary>
internal sealed class ValidatorValueHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   TokenizationEvent sourceEvent, BuildContext context)
    {
        if (type != DiagnosticIssueType.ValidatorRejection)
            return null;

        var decoratorName = sourceEvent.DecoratorName;
        var value = sourceEvent.Value ?? string.Empty;

        if (decoratorName == null)
            return null;

        if (decoratorName.Contains("IsEmail", StringComparison.Ordinal))
        {
            if (!value.Contains("@", StringComparison.Ordinal))
                return $"Value '{value}' does not contain '@'";
            return null;
        }

        if (decoratorName.Contains("IsDomainName", StringComparison.Ordinal))
        {
            if (value.Contains(" ", StringComparison.Ordinal))
                return $"Value '{value}' contains spaces";
            return $"Value '{value}' does not appear to be a domain name";
        }

        if (decoratorName.Contains("IsNumeric", StringComparison.Ordinal))
        {
            return $"Value '{value}' is not a valid number";
        }

        if (decoratorName.Contains("IsPhoneNumber", StringComparison.Ordinal))
        {
            return $"Value '{value}' may contain non-phone characters";
        }

        return null;
    }
}
