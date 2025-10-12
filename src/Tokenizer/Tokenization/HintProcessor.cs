using System.Linq;
using Tokens.Enumerators;
using Tokens.Logging;

namespace Tokens.Tokenization
{
    /// <summary>
    /// Hint processor that finds and validates hints in input text according to template hint definitions.
    /// This service encapsulates hint finding logic, hint matching, and missing hint detection.
    /// </summary>
    public class HintProcessor : IHintProcessor
    {
        private readonly ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="HintProcessor"/> class.
        /// </summary>
        public HintProcessor()
        {
            log = LogProvider.For<HintProcessor>();
        }

        /// <summary>
        /// Finds all hints defined in the template within the input text and validates them.
        /// </summary>
        /// <param name="template">The template containing hint definitions</param>
        /// <param name="enumerator">The token enumerator positioned at the start of input</param>
        /// <param name="result">The result object to populate with hint matches and misses</param>
        /// <returns>True if any required hints are missing, false if all required hints are found</returns>
        public bool FindAndValidateHints(
            Template template, 
            TokenEnumerator enumerator, 
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            if (template.Hints.Count == 0) return false;

            while (enumerator.IsEmpty == false)
            {
                // Check hints
                foreach (var hint in template.Hints)
                {
                    if (IsHintMatch(hint, enumerator) && AddHintMatch(hint, enumerator, result))
                    {
                        log.Verbose("  -> Ln:{0} Col:{1} Found Hint: {2}", enumerator.Location.Line, enumerator.Location.Column, hint.Text);
                    }
                }

                // Exit early if all hints found
                if (result.Hints.Matches.Count == template.Hints.Count) break;

                enumerator.Next();
            }

            // Build unmatched hint collection
            foreach (var hint in template.Hints)
            {
                if (AddHintMiss(hint, result))
                {
                    log.Verbose("  -> Missing Hint: {0}", hint.Text);
                }
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
                return false;

            return enumerator.Match(hint.Text);
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

            return result.Hints.AddMatch(hint, enumerator);
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

            return result.Hints.AddMiss(hint);
        }

        /// <summary>
        /// Resets the enumerator to the beginning of the input after hint processing is complete.
        /// </summary>
        /// <param name="enumerator">The token enumerator to reset</param>
        public void ResetEnumeratorAfterHintProcessing(TokenEnumerator enumerator)
        {
            ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));

            enumerator.Reset();
        }
    }
}
