using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Tokens.Tokenization
{
    /// <summary>
    /// Tracks tokenization progress on a per-line basis for line-by-line summary logging.
    /// </summary>
    internal class LineTracker
    {
        private readonly ILogger log;
        private readonly Template template;
        private int currentLine;
        private readonly List<string> matchedTokensOnLine;

        public LineTracker(Template template, ILogger log)
        {
            this.template = template;
            this.log = log;
            this.currentLine = 1;
            this.matchedTokensOnLine = new List<string>();
        }

        /// <summary>
        /// Records a token match on the current line.
        /// </summary>
        /// <param name="tokenName">The name of the matched token</param>
        /// <param name="line">The line number where the match occurred</param>
        /// <param name="matchIds">Set of all matched token IDs</param>
        public void RecordMatch(string tokenName, int line, HashSet<int> matchIds)
        {
            CheckLineTransition(line, matchIds);
            matchedTokensOnLine.Add(tokenName);
        }

        /// <summary>
        /// Finalizes tracking and logs the last line summary if needed.
        /// </summary>
        /// <param name="matchIds">Set of all matched token IDs</param>
        public void Finalize(HashSet<int> matchIds)
        {
            if (matchedTokensOnLine.Count > 0)
            {
                LogLineSummary(currentLine, matchIds);
            }
        }

        /// <summary>
        /// Checks if we've transitioned to a new line and logs the previous line's summary.
        /// </summary>
        private void CheckLineTransition(int newLine, HashSet<int> matchIds)
        {
            if (newLine > currentLine)
            {
                if (matchedTokensOnLine.Count > 0)
                {
                    LogLineSummary(currentLine, matchIds);
                }

                currentLine = newLine;
                matchedTokensOnLine.Clear();
            }
        }

        /// <summary>
        /// Logs the summary for a completed line.
        /// </summary>
        private void LogLineSummary(int line, HashSet<int> matchIds)
        {
            var remainingTokens = template.Tokens
                .Where(t => !matchIds.Contains(t.Id) && t.Required)
                .Select(t => t.Name)
                .ToList();

            var matchedList = string.Join(", ", matchedTokensOnLine);
            var remainingList = remainingTokens.Any() ? string.Join(", ", remainingTokens) : "none";

            log.LogInformation("Line {Line} complete: Matched [{Matched}], Remaining [{Remaining}]",
                line, matchedList, remainingList);
        }
    }
}
