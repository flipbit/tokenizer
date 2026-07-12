using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// Result builder that creates and populates tokenization result objects with matches, misses, and exception information.
/// This service encapsulates result object creation, token match/miss management, and exception collection.
/// </summary>
internal sealed class ResultBuilder : IResultBuilder
{
    private readonly ILogger<ResultBuilder> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultBuilder"/> class.
    /// </summary>
    public ResultBuilder() : this(logger: null)
    {
    }

    public ResultBuilder(ILogger<ResultBuilder>? logger)
    {
        _log = logger ?? NullLogger<ResultBuilder>.Instance;
    }

    /// <summary>
    /// Builds the collection of unmatched tokens by comparing template tokens
    /// against the tokens that were successfully matched.
    /// </summary>
    /// <param name="template">The template containing all token definitions</param>
    /// <param name="result">The result object to populate with unmatched tokens</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    public void BuildUnmatchedTokens(
        Template template,
        TokenizeResult result,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Building unmatched tokens for template: TemplateName={TemplateName}", template.Name);
        }

        var matchedIds = new HashSet<int>(result.Tokens.Matches.Select(m => m.Token.Id));
        var unmatchedCount = 0;
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var token in template.Tokens)
        {
            if (!matchedIds.Contains(token.Id))
            {
                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug(
                        "Token not matched: TokenId={TokenId}, TokenName={TokenName}, Required={Required}",
                        token.Id,
                        token.Name,
                        token.IsRequired);
                }

                collector.Record(TokenizationEventType.TokenMissed,
                    tokenName: token.Name, tokenId: token.Id,
                    detail: token.Preamble);

                result.Tokens.AddMiss(token);
                unmatchedCount++;
            }
        }

        var matchCount = result.Tokens.Matches.Count;
        var requiredMissCount = result.Tokens.Misses.Count(t => t.IsRequired);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug(
                "Tokenization results summary: TotalMatches={TotalMatches}, TotalMisses={TotalMisses}, RequiredMisses={RequiredMisses}, Success={Success}",
                matchCount,
                unmatchedCount,
                requiredMissCount,
                result.Success);
        }
    }
}
