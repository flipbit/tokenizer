namespace Tokens.Diagnostics;

/// <summary>
/// Categories of issues that can be identified during tokenization diagnostics.
/// Used for programmatic filtering and classification of diagnostic issues.
/// </summary>
public enum DiagnosticIssueType
{
    /// <summary>
    /// A required token's preamble was never found in the input.
    /// The template expected to find a specific string but it was absent.
    /// </summary>
    PreambleNeverFound,

    /// <summary>
    /// A token's preamble was found but the extracted value failed validation.
    /// A validator decorator rejected the accumulated value.
    /// </summary>
    ValidatorRejection,

    /// <summary>
    /// A token's preamble was found but a transformer failed on the extracted value.
    /// A transformer decorator could not convert the accumulated value.
    /// </summary>
    TransformerFailure,

    /// <summary>
    /// A token was matched but assigned an unexpected or empty value,
    /// suggesting the template consumed too much or too little input.
    /// </summary>
    ValueMismatch,

    /// <summary>
    /// A repeating token was disabled prematurely due to a line gap or
    /// failed repetition, resulting in fewer matches than expected.
    /// </summary>
    RepeatingTokenCutShort,

    /// <summary>
    /// Input text exists that doesn't correspond to any token in the template,
    /// which may have pushed subsequent tokens out of alignment.
    /// </summary>
    UnmatchedInputSection,

    /// <summary>
    /// A required hint string was not found in the input text,
    /// causing tokenization to be skipped entirely.
    /// </summary>
    HintMissing,
}
