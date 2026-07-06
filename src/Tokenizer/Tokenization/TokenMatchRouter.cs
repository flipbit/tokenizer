using System.Linq;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Routes each character to the appropriate processing path during tokenization.
/// Handles the per-character decision: repeated token, newline-terminated, new match, or accumulate.
/// </summary>
internal sealed class TokenMatchRouter
{
    private readonly Template template;
    private readonly CandidateProcessor candidateProcessor;
    private readonly IDiagnosticCollector collector;
    private readonly IHintStrategy? hintStrategy;

    public TokenMatchRouter(
        Template template,
        CandidateProcessor candidateProcessor,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy)
    {
        this.template = template;
        this.candidateProcessor = candidateProcessor;
        this.collector = collector;
        this.hintStrategy = hintStrategy;
    }

    /// <summary>
    /// Examines the next character in the input and routes to the appropriate handler.
    /// Returns false if the repeated-token path cleared candidates (caller should continue the loop).
    /// Returns true for all other paths.
    /// </summary>
    public bool RouteNext(TokenizationContext context)
    {
        var next = context.Enumerator.Peek();

        // Check for repeated current token
        if (context.Candidates.HasCandidates &&
            context.Enumerator.TryMatch(context.Candidates.Preamble) &&
            context.Candidates.Preamble.Length > 0)
        {
            if (!candidateProcessor.HandleRepeat(context))
            {
                return false;
            }
        }

        // Assign newline terminated token
        if (context.Candidates.HasCandidates && context.Candidates.TerminateOnNewLine && next == '\n')
        {
            candidateProcessor.HandleNewline(context);
            return true;
        }

        // Check for next token
        if (context.Enumerator.TryMatch(
            template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds),
            template.Options.OutOfOrderTokens,
            context.MatchBuffer))
        {
            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.PreambleMatched,
                    tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                    location: context.Enumerator.Location);
            }

            // Notify hint strategy of matched tokens
            if (hintStrategy != null)
            {
                foreach (var match in context.MatchBuffer)
                {
                    hintStrategy.OnTokenMatched(match);
                }
            }

            // First token found — prepare to read token value
            if (context.Candidates.HasCandidates == false)
            {
                context.Candidates.AddRange(context.MatchBuffer);
                context.ClearReplacement();
                context.Enumerator.Advance(context.Candidates.Preamble.Length);
                return true;
            }

            // Switch if we've accumulated a value — otherwise consume a character first
            if (context.Replacement.Length > 0)
            {
                candidateProcessor.TryAssign(context, context.ReplacementLocation);

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

        return true;
    }
}
