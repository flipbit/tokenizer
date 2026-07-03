using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Hint processor that finds and validates hints in input text according to template hint definitions.
/// This service encapsulates hint finding logic, hint matching, and missing hint detection.
/// </summary>
internal class HintProcessor : IHintProcessor
{
    private readonly ILogger<HintProcessor> log;

    /// <summary>
    /// Initializes a new instance of the <see cref="HintProcessor"/> class.
    /// </summary>
    public HintProcessor() : this(null)
    {
    }

    public HintProcessor(ILogger<HintProcessor>? logger)
    {
        log = logger ?? NullLogger<HintProcessor>.Instance;
    }

    /// <summary>
    /// Finds all hints defined in the template within the input text and validates them.
    /// </summary>
    /// <param name="template">The template containing hint definitions</param>
    /// <param name="enumerator">The token enumerator positioned at the start of input</param>
    /// <param name="result">The result object to populate with hint matches and misses</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <returns>True if any required hints are missing, false if all required hints are found</returns>
    public bool FindAndValidateHints(
        Template template,
        TokenEnumerator enumerator,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        if (template.Hints.Count == 0)
        {
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("No hints defined in template, skipping hint processing");
            }
            return false;
        }

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Starting hint processing with {HintCount} hint(s) defined", template.Hints.Count);
        }

        while (enumerator.IsEmpty == false)
        {
            // Check hints
            foreach (var hint in template.Hints)
            {
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Checking hint '{HintText}' at position Line:{Line} Col:{Column}",
                        hint.Text, enumerator.Location.Line, enumerator.Location.Column);
                }

                if (IsHintMatch(hint, enumerator) && AddHintMatch(hint, enumerator, result))
                {
                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Hint matched and added: '{HintText}' at Line:{Line} Col:{Column}, Optional:{Optional}",
                            hint.Text, enumerator.Location.Line, enumerator.Location.Column, hint.Optional);
                    }

                    collector.Record(DiagnosticEventType.HintMatched,
                        value: hint.Text,
                        location: enumerator.Location);
                }
            }

            // Exit early if all hints found
            if (result.Hints.Matches.Count == template.Hints.Count)
            {
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("All {HintCount} hint(s) found, ending search early", template.Hints.Count);
                }
                break;
            }

            enumerator.Next();
        }

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Hint search complete. Found {MatchCount} of {TotalCount} hint(s)",
                result.Hints.Matches.Count, template.Hints.Count);
        }

        // Build unmatched hint collection
        foreach (var hint in template.Hints)
        {
            if (AddHintMiss(hint, result))
            {
                if (hint.Optional)
                {
                    log.LogWarning("Optional hint not found: '{HintText}'", hint.Text);
                }
                else
                {
                    log.LogError("Required hint missing: '{HintText}'", hint.Text);

                    collector.Record(DiagnosticEventType.HintMissing,
                        value: hint.Text);
                }
            }
        }

        var missingRequiredCount = result.Hints.Misses.Count(h => h.Optional == false);
        var missingOptionalCount = result.Hints.Misses.Count(h => h.Optional);

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Hint validation complete. Missing required: {MissingRequired}, Missing optional: {MissingOptional}",
                missingRequiredCount, missingOptionalCount);
        }

        ResetEnumeratorAfterHintProcessing(enumerator);

        return result.Hints.Misses.Any(h => h.Optional == false);
    }

    /// <summary>
    /// Checks if a specific hint text matches at the current enumerator position.
    /// </summary>
    /// <param name="hint">The hint to check for</param>
    /// <param name="enumerator">The token enumerator at the position to check</param>
    /// <returns>True if the hint matches at the current position</returns>
    public bool IsHintMatch(
        Hint hint,
        TokenEnumerator enumerator)
    {
        ArgumentValidation.ThrowIfNull(hint, nameof(hint));
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));

        // Return false for null or empty hint text
        if (string.IsNullOrEmpty(hint.Text))
        {
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Hint validation failed: hint text is null or empty");
            }
            return false;
        }

        var isMatch = enumerator.TryMatch(hint.Text);
        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Hint match validation for '{HintText}': {IsMatch}", hint.Text, isMatch);
        }

        return isMatch;
    }

    /// <summary>
    /// Adds a hint match to the result and advances the enumerator past the matched hint.
    /// </summary>
    /// <param name="hint">The hint that was matched</param>
    /// <param name="enumerator">The token enumerator positioned at the hint</param>
    /// <param name="result">The result object to add the match to</param>
    /// <returns>True if the hint was successfully added as a match</returns>
    public bool AddHintMatch(
        Hint hint,
        TokenEnumerator enumerator,
        TokenizeResultBase result)
    {
        ArgumentValidation.ThrowIfNull(hint, nameof(hint));
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        var added = result.Hints.AddMatch(hint, enumerator);

        if (log.IsEnabled(LogLevel.Trace))
        {
            if (added)
            {
                log.LogTrace("Successfully added hint match for '{HintText}' to result", hint.Text);
            }
            else
            {
                log.LogTrace("Hint match for '{HintText}' was not added (likely already exists)", hint.Text);
            }
        }

        return added;
    }

    /// <summary>
    /// Adds a hint miss to the result for hints that were not found in the input.
    /// </summary>
    /// <param name="hint">The hint that was not found</param>
    /// <param name="result">The result object to add the miss to</param>
    /// <returns>True if the hint was successfully added as a miss</returns>
    public bool AddHintMiss(
        Hint hint,
        TokenizeResultBase result)
    {
        ArgumentValidation.ThrowIfNull(hint, nameof(hint));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        var added = result.Hints.AddMiss(hint);

        if (log.IsEnabled(LogLevel.Trace))
        {
            if (added)
            {
                log.LogTrace("Added hint miss for '{HintText}', Optional:{Optional}", hint.Text, hint.Optional);
            }
            else
            {
                log.LogTrace("Hint '{HintText}' was already matched, not adding as miss", hint.Text);
            }
        }

        return added;
    }

    /// <summary>
    /// Resets the enumerator to the beginning of the input after hint processing is complete.
    /// </summary>
    /// <param name="enumerator">The token enumerator to reset</param>
    public void ResetEnumeratorAfterHintProcessing(TokenEnumerator enumerator)
    {
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));

        if (enumerator.CanReset)
        {
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Resetting enumerator after hint processing");
            }
            enumerator.Reset();
        }
        else
        {
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Skipping enumerator reset — TextReader-based enumerator does not support reset");
            }
        }
    }
}
