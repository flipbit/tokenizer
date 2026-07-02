using System.Globalization;

namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for TransformerFailed events caused by ToDateTime
/// or ToDateTimeUtc transformers by trying common date format patterns
/// against the failed value and suggesting the matching format.
/// </summary>
internal sealed class DateFormatHintGenerator : IHintGenerator
{
    private static readonly string[] CommonFormats =
    {
        "yyyy-MM-dd", "dd-MM-yyyy", "MM-dd-yyyy",
        "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd",
        "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy HH:mm:ss", "MM-dd-yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss", "yyyy/MM/dd HH:mm:ss",
        "dd-MMM-yyyy", "MMM dd, yyyy",
        "dd-MMM-yyyy HH:mm:ss", "MMM dd, yyyy HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ",
    };

    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   TokenizationDiagnostics trace)
    {
        if (sourceEvent.DecoratorName == null ||
            !sourceEvent.DecoratorName.Contains("ToDateTime"))
        {
            return null;
        }

        var value = sourceEvent.Value;

        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var originalFormat = sourceEvent.DecoratorArgs != null && sourceEvent.DecoratorArgs.Length > 0
            ? sourceEvent.DecoratorArgs[0]
            : null;

        foreach (var format in CommonFormats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out _))
            {
                if (originalFormat != null)
                {
                    return $"Value '{value}' matches format '{format}'. " +
                           $"Change transformer to use '{format}' instead of '{originalFormat}'.";
                }

                return $"Value '{value}' matches format '{format}'.";
            }
        }

        return null;
    }
}
