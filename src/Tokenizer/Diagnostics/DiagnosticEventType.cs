namespace Tokens.Diagnostics;

/// <summary>
/// Identifies the type of decision or event recorded during tokenization.
/// Every event type has a corresponding diagnostic meaning that AI agents
/// and developers can use to understand the tokenization process.
/// </summary>
public enum DiagnosticEventType
{
    /// <summary>
    /// Tokenization has started for a template/input pair.
    /// Detail contains template name, token count, and input length.
    /// </summary>
    TokenizationStarted,

    /// <summary>
    /// Tokenization has completed for a template/input pair.
    /// Detail contains final match count, miss count, and success status.
    /// </summary>
    TokenizationCompleted,

    /// <summary>
    /// A required hint string was found in the input text.
    /// Value contains the hint string that was matched.
    /// </summary>
    HintMatched,

    /// <summary>
    /// A required hint string was not found in the input text.
    /// Tokenization will be skipped. Value contains the missing hint string.
    /// </summary>
    HintMissing,

    /// <summary>
    /// The engine began searching for a token's preamble string in the input.
    /// TokenName identifies the token(s) being searched for.
    /// Location is the current position in the input.
    /// </summary>
    PreambleSearchStarted,

    /// <summary>
    /// A token's preamble string was found at the current input position.
    /// TokenName and Location identify what matched and where.
    /// </summary>
    PreambleMatched,

    /// <summary>
    /// No token preamble matched at the current input position.
    /// The engine will consume one character and advance.
    /// Location is the position where matching was attempted.
    /// </summary>
    PreambleNotFound,

    /// <summary>
    /// A value has been accumulated for the current candidate token(s).
    /// Value contains the accumulated string. Emitted when the value
    /// is about to be used (before assignment), not per-character.
    /// </summary>
    ValueAccumulated,

    /// <summary>
    /// The engine is attempting to assign an accumulated value to one
    /// or more candidate tokens. Value contains the string being tested.
    /// TokenName lists the candidate token names.
    /// </summary>
    TokenAssignmentAttempted,

    /// <summary>
    /// A token's validator decorator accepted the value.
    /// DecoratorName identifies the validator (e.g. "IsEmail").
    /// Value contains the input that was validated.
    /// </summary>
    ValidatorPassed,

    /// <summary>
    /// A token's validator decorator rejected the value.
    /// DecoratorName identifies the validator. Value contains the rejected input.
    /// This causes the token assignment to fail.
    /// </summary>
    ValidatorFailed,

    /// <summary>
    /// A token's transformer decorator successfully transformed the value.
    /// DecoratorName identifies the transformer (e.g. "ToDateTimeUtc").
    /// DecoratorArgs contains the transformer parameters (e.g. ["yyyy-MM-dd"]).
    /// Value contains the input before transformation.
    /// Detail contains the output after transformation.
    /// </summary>
    TransformerSucceeded,

    /// <summary>
    /// A token's transformer decorator failed to transform the value.
    /// DecoratorName identifies the transformer. DecoratorArgs contains parameters.
    /// Value contains the input that could not be transformed.
    /// This causes the token assignment to fail.
    /// </summary>
    TransformerFailed,

    /// <summary>
    /// A token was successfully assigned a value from the input.
    /// TokenName is the assigned token. Value is the final assigned value
    /// (after all transformations). Location is where it was found in the input.
    /// </summary>
    TokenAssigned,

    /// <summary>
    /// None of the candidate tokens could accept the accumulated value.
    /// All validators/transformers in the candidate list rejected it.
    /// Value contains the rejected string. TokenName lists the candidates.
    /// </summary>
    TokenAssignmentFailed,

    /// <summary>
    /// A newline-terminated token's value was processed at a newline boundary.
    /// TokenName identifies the token. Value contains the extracted value.
    /// </summary>
    NewlineTerminatedTokenProcessed,

    /// <summary>
    /// The engine is backtracking because no candidate tokens can accept
    /// the current accumulated value. The engine will advance past the
    /// preamble and retry matching. Location is the backtrack position.
    /// </summary>
    BacktrackStarted,

    /// <summary>
    /// A repeating token has been disabled and will no longer match.
    /// This occurs when a repeating token was the last match but failed
    /// to match the next repetition, or when a line gap was detected.
    /// TokenName identifies the disabled token.
    /// </summary>
    RepeatingTokenDisabled,

    /// <summary>
    /// A ConsiderOnce token failed to match and has been permanently
    /// removed from the candidate list and recorded as a miss.
    /// TokenName identifies the removed token.
    /// </summary>
    ConsiderOnceTokenRemoved,

    /// <summary>
    /// A front matter token was successfully assigned its value.
    /// TokenName is the token name. Value is the assigned value.
    /// </summary>
    FrontMatterTokenAssigned,

    /// <summary>
    /// A front matter token failed to assign its value.
    /// TokenName is the token name.
    /// </summary>
    FrontMatterTokenFailed,

    /// <summary>
    /// A required or optional token was never matched during tokenization.
    /// Emitted during the post-tokenization summary phase.
    /// TokenName identifies the unmatched token.
    /// </summary>
    TokenMissed,
}
