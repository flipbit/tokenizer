namespace Tokens.Diagnostics.Hints
{
    /// <summary>
    /// Generates a hint for ValidatorRejection issues by explaining why the value
    /// failed validation for known validator types.
    /// </summary>
    internal sealed class ValidatorValueHintGenerator : IHintGenerator
    {
        /// <inheritdoc />
        public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                       TokenizationDiagnostics trace)
        {
            if (issue.Type != DiagnosticIssueType.ValidatorRejection)
                return null;

            var decoratorName = sourceEvent.DecoratorName;
            var value = sourceEvent.Value ?? string.Empty;

            if (decoratorName == null)
                return null;

            if (decoratorName.Contains("IsEmail"))
            {
                if (!value.Contains("@"))
                    return $"Value '{value}' does not contain '@'";
                return null;
            }

            if (decoratorName.Contains("IsDomainName"))
            {
                if (value.Contains(" "))
                    return $"Value '{value}' contains spaces";
                return $"Value '{value}' does not appear to be a domain name";
            }

            if (decoratorName.Contains("IsNumeric"))
            {
                return $"Value '{value}' is not a valid number";
            }

            if (decoratorName.Contains("IsPhoneNumber"))
            {
                return $"Value '{value}' may contain non-phone characters";
            }

            return null;
        }
    }
}
