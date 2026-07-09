using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Routes each character to the appropriate processing path during tokenization.
/// Handles the per-character decision: repeated token, newline-terminated, new match, or accumulate.
/// </summary>
internal sealed class TokenMatchRouter
{
    private readonly Template _template;
    private readonly CandidateProcessor _candidateProcessor;
    private readonly IDiagnosticCollector _collector;

    public TokenMatchRouter(
        Template template,
        CandidateProcessor candidateProcessor,
        IDiagnosticCollector collector)
    {
        _template = template;
        _candidateProcessor = candidateProcessor;
        _collector = collector;
    }

    /// <summary>
    /// Examines the next character in the input and routes to the appropriate handler.
    /// </summary>
    public void RouteNext(TokenizationContext context)
    {
        var next = context.Enumerator.Peek();

        // Check for repeated current token
        // CodeQL cs/nested-if-statements: outer if tests preconditions (candidates + preamble match),
        // inner if acts on the result of HandleRepeat — distinct concerns that read better separated
        if (context.Candidates.HasCandidates &&
            context.Enumerator.TryMatch(context.Candidates.Preamble) &&
            context.Candidates.Preamble.Length > 0)
        {
            if (!_candidateProcessor.HandleRepeat(context))
            {
                return;
            }
        }

        // Assign newline terminated token
        if (context.Candidates.HasCandidates && context.Candidates.TerminateOnNewLine && next == '\n')
        {
            _candidateProcessor.HandleNewline(context);
            return;
        }

        // Check for next token
        if (context.Enumerator.TryMatch(
            _template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds),
            _template.Options.OutOfOrderTokens,
            context.MatchBuffer))
        {
            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.PreambleMatched,
                    tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                    location: context.Enumerator.Location);
            }

            // First token found — prepare to read token value
            if (!context.Candidates.HasCandidates)
            {
                context.Candidates.AddRange(context.MatchBuffer);
                context.ClearReplacement();
                context.Enumerator.Advance(context.Candidates.Preamble.Length);
                return;
            }

            // Switch if we've accumulated a value — otherwise consume a character first
            if (context.Replacement.Length > 0)
            {
                _candidateProcessor.TryAssign(context, context.ReplacementLocation);

                context.ClearCandidates();
                context.Candidates.AddRange(context.MatchBuffer);
                context.ClearReplacement();
                context.Enumerator.Advance(context.Candidates.Preamble.Length);
                context.ReplacementLocation = context.Enumerator.Location;
            }
            else
            {
                context.Replacement.Append(next);
                context.Enumerator.Next();
            }
        }
        else
        {
            context.Replacement.Append(next);
            context.Enumerator.Next();
        }
    }
}
