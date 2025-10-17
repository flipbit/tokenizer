using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Enumerators;

namespace Tokens.Tokenization
{
    /// <summary>
    /// Result builder that creates and populates tokenization result objects with matches, misses, and exception information.
    /// This service encapsulates result object creation, token match/miss management, and exception collection.
    /// </summary>
    public class ResultBuilder : IResultBuilder
    {
        private readonly ILogger<ResultBuilder> log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultBuilder"/> class.
        /// </summary>
        public ResultBuilder() : this(null)
        {
        }

        public ResultBuilder(ILogger<ResultBuilder>? logger)
        {
            log = logger ?? NullLogger<ResultBuilder>.Instance;
        }

        /// <summary>
        /// Creates a new TokenizeResult instance for the given template.
        /// </summary>
        /// <param name="template">The template used for tokenization</param>
        /// <returns>A new TokenizeResult instance</returns>
        public TokenizeResult CreateTokenizeResult(Template template)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));

            return new TokenizeResult(template);
        }

        /// <summary>
        /// Creates a new TokenizeResult&lt;T&gt; instance for the given template.
        /// </summary>
        /// <typeparam name="T">The type of object to populate</typeparam>
        /// <param name="template">The template used for tokenization</param>
        /// <returns>A new TokenizeResult&lt;T&gt; instance</returns>
        public TokenizeResult<T> CreateTokenizeResult<T>(Template template) where T : class, new()
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));

            return new TokenizeResult<T>(template);
        }

        /// <summary>
        /// Adds a token match to the result with the assigned value and location.
        /// </summary>
        /// <param name="token">The token that was matched</param>
        /// <param name="assignedValue">The value that was assigned to the token</param>
        /// <param name="location">The location where the token was found</param>
        /// <param name="result">The result object to add the match to</param>
        public void AddTokenMatch(
            Token token,
            object assignedValue,
            FileLocation location,
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(token, nameof(token));
            ArgumentValidation.ThrowIfNull(assignedValue, nameof(assignedValue));
            ArgumentValidation.ThrowIfNull(location, nameof(location));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            log.LogTrace(
                "Adding token match: TokenId={TokenId}, TokenName={TokenName}, Value={Value}, Line={Line}, Column={Column}",
                token.Id,
                token.Name,
                assignedValue,
                location.Line,
                location.Column);

            result.Tokens.AddMatch(token, assignedValue, location);
        }

        /// <summary>
        /// Adds a token miss to the result for tokens that were not found.
        /// </summary>
        /// <param name="token">The token that was not found</param>
        /// <param name="result">The result object to add the miss to</param>
        public void AddTokenMiss(
            Token token,
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(token, nameof(token));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            log.LogTrace(
                "Adding token miss: TokenId={TokenId}, TokenName={TokenName}, Required={Required}",
                token.Id,
                token.Name,
                token.Required);

            result.Tokens.AddMiss(token);
        }

        /// <summary>
        /// Adds an exception to the result for errors that occurred during tokenization.
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="result">The result object to add the exception to</param>
        public void AddException(
            Exception exception, 
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(exception, nameof(exception));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            result.Exceptions.Add(exception);
        }

        /// <summary>
        /// Builds the collection of unmatched tokens by comparing template tokens
        /// against the tokens that were successfully matched.
        /// </summary>
        /// <param name="template">The template containing all token definitions</param>
        /// <param name="result">The result object to populate with unmatched tokens</param>
        public void BuildUnmatchedTokens(
            Template template,
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            log.LogDebug("Building unmatched tokens for template: TemplateName={TemplateName}", template.Name);

            var unmatchedCount = 0;
            foreach (var token in template.Tokens)
            {
                if (result.Tokens.Matches.Any(m => m.Token.Id == token.Id) == false)
                {
                    log.LogDebug(
                        "Token not matched: TokenId={TokenId}, TokenName={TokenName}, Required={Required}",
                        token.Id,
                        token.Name,
                        token.Required);

                    result.Tokens.Misses.Add(token);
                    unmatchedCount++;
                }
            }

            var matchCount = result.Tokens.Matches.Count;
            var requiredMissCount = result.Tokens.Misses.Count(t => t.Required);

            log.LogDebug(
                "Tokenization results summary: TotalMatches={TotalMatches}, TotalMisses={TotalMisses}, RequiredMisses={RequiredMisses}, Success={Success}",
                matchCount,
                unmatchedCount,
                requiredMissCount,
                result.Success);
        }

        /// <summary>
        /// Adds matched token IDs to the tracking set for template ordering logic.
        /// </summary>
        /// <param name="template">The template containing token definitions</param>
        /// <param name="matchedToken">The token that was matched</param>
        /// <param name="matchIds">The set of matched token IDs to update</param>
        public void AddMatchedTokenIds(
            Template template, 
            Token matchedToken, 
            HashSet<int> matchIds)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            ArgumentValidation.ThrowIfNull(matchedToken, nameof(matchedToken));
            ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));

            var tokenIdsToAdd = template.GetTokenIdsUpTo(matchedToken);
            
            foreach (var tokenId in tokenIdsToAdd)
            {
                if (matchIds.Contains(tokenId) == false)
                {
                    matchIds.Add(tokenId);
                }
            }
        }

        /// <summary>
        /// Checks if a token was the last matched token in the result.
        /// </summary>
        /// <param name="result">The result object to check</param>
        /// <param name="token">The token to check</param>
        /// <returns>True if the token was the last matched token</returns>
        public bool WasLastMatchedToken(
            TokenizeResultBase result, 
            Token token)
        {
            ArgumentValidation.ThrowIfNull(result, nameof(result));
            ArgumentValidation.ThrowIfNull(token, nameof(token));

            var lastMatch = result.Tokens.Matches.LastOrDefault();

            if (lastMatch != null)
            {
                return lastMatch.Token.Id == token.Id;
            }

            return false;
        }
    }
}
