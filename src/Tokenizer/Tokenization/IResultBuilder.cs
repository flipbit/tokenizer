using System;
using System.Collections.Generic;
using Tokens.Enumerators;

namespace Tokens.Tokenization
{
    /// <summary>
    /// Defines the result builder that creates and populates tokenization result objects
    /// with matches, misses, and exception information.
    /// </summary>
    public interface IResultBuilder
    {
        /// <summary>
        /// Creates a new TokenizeResult instance for the given template.
        /// </summary>
        /// <param name="template">The template used for tokenization</param>
        /// <returns>A new TokenizeResult instance</returns>
        TokenizeResult CreateTokenizeResult(Template template);

        /// <summary>
        /// Creates a new TokenizeResult&lt;T&gt; instance for the given template.
        /// </summary>
        /// <typeparam name="T">The type of object to populate</typeparam>
        /// <param name="template">The template used for tokenization</param>
        /// <returns>A new TokenizeResult&lt;T&gt; instance</returns>
        TokenizeResult<T> CreateTokenizeResult<T>(Template template) where T : class, new();

        /// <summary>
        /// Adds a token match to the result with the assigned value and location.
        /// </summary>
        /// <param name="token">The token that was matched</param>
        /// <param name="assignedValue">The value that was assigned to the token</param>
        /// <param name="location">The location where the token was found</param>
        /// <param name="result">The result object to add the match to</param>
        void AddTokenMatch(Token token, object assignedValue, FileLocation location, TokenizeResultBase result);

        /// <summary>
        /// Adds a token miss to the result for tokens that were not found.
        /// </summary>
        /// <param name="token">The token that was not found</param>
        /// <param name="result">The result object to add the miss to</param>
        void AddTokenMiss(Token token, TokenizeResultBase result);

        /// <summary>
        /// Adds an exception to the result for errors that occurred during tokenization.
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="result">The result object to add the exception to</param>
        void AddException(Exception exception, TokenizeResultBase result);

        /// <summary>
        /// Builds the collection of unmatched tokens by comparing template tokens
        /// against the tokens that were successfully matched.
        /// </summary>
        /// <param name="template">The template containing all token definitions</param>
        /// <param name="result">The result object to populate with unmatched tokens</param>
        void BuildUnmatchedTokens(Template template, TokenizeResultBase result);

        /// <summary>
        /// Adds matched token IDs to the tracking set for template ordering logic.
        /// </summary>
        /// <param name="template">The template containing token definitions</param>
        /// <param name="matchedToken">The token that was matched</param>
        /// <param name="matchIds">The set of matched token IDs to update</param>
        void AddMatchedTokenIds(Template template, Token matchedToken, HashSet<int> matchIds);

        /// <summary>
        /// Checks if a token was the last matched token in the result.
        /// </summary>
        /// <param name="result">The result object to check</param>
        /// <param name="token">The token to check</param>
        /// <returns>True if the token was the last matched token</returns>
        bool WasLastMatchedToken(TokenizeResultBase result, Token token);
    }
}
