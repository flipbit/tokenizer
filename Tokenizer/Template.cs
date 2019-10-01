using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Extensions;

namespace Tokens
{
    /// <summary>
    /// Represents a template to use to extract data from
    /// free text.
    /// </summary>
    public class Template
    {
        private readonly List<Token> tokens;
        private string name;

        public Template()
        {
            tokens = new List<Token>();
            Hints = new List<Hint>();
            Tags = new List<string>();
        }

        public Template(string name, string content) : this()
        {
            Name = name;
            Content = content;
        }

        /// <summary>
        /// The template content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The name of the template.  If no name is specified, the name will be assigned
        /// a hash of the template content.
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = Content.ToMd5();
                }

                return name;
            }
            set => name = value;
        }

        /// <summary>
        /// Contains the hints associated with this <see cref="Template"/>.
        /// A <see cref="Hint"/> is used to select the best matching template by the <see cref="TokenMatcher"/> based
        /// on text found within the input string.
        /// </summary>
        public IList<Hint> Hints { get; }

        /// <summary>
        /// Contains the tags associated with this <see cref="Template"/>.
        /// A tag is used to select the best matching template by the <see cref="TokenMatcher"/> based on tags passed
        /// in with the input string.
        /// </summary>
        public IList<string> Tags { get; }

        /// <summary>
        /// The tokens contained within the template
        /// </summary>
        public IReadOnlyCollection<Token> Tokens => tokens.AsReadOnly();

        /// <summary>
        /// Contains the <see cref="TokenizerOptions"/> used when parsing this <see cref="Template"/>.
        /// </summary>
        public TokenizerOptions Options { get; set; }

        /// <summary>
        /// Determines if this instance contains the given tag.
        /// </summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;

            foreach (var candidate in Tags)
            {
                if (string.Compare(candidate, tag, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if this instance contains all of the given tags.
        /// </summary>
        public bool HasTags(IList<string> tags)
        {
            return HasTags(tags, out _);
        }

        /// <summary>
        /// Determines if this instance contains all of the given tags.
        /// </summary>
        public bool HasTags(IList<string> tags, out IList<string> missing)
        {
            missing = new List<string>();

            if (tags == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (HasTag(tag) == false)
                {
                    missing.Add(tag);
                }
            }

            return missing.Count == 0;
        }

        internal bool HasOnlyFrontMatterTokens => tokens.Where(t => !string.IsNullOrWhiteSpace(t.Name)).All(t => t.IsFrontMatterToken);

        internal IEnumerable<int> GetTokenIdsUpTo(Token token)
        {
            var matchIds = new List<int>();

            // Only remove match if out-of-order token
            if (Options.OutOfOrderTokens)
            {
                if (token.Repeating == false) matchIds.Add(token.Id);
                return matchIds;
            }

            foreach (var candidate in tokens)
            {
                if (candidate == token)
                {
                    if (candidate.Repeating == false)
                    {
                        matchIds.Add(candidate.Id);
                    }
                    break;
                }

                matchIds.Add(candidate.Id);
            }

            return matchIds;
        }

        internal void AddToken(Token token)
        {
            tokens.Add(token);
        }

        internal IEnumerable<Token> TokensExcluding(IEnumerable<int> tokenIds)
        {
            var includedTokens = tokens
                .Where(t => t.IsFrontMatterToken == false)
                .Where(t => tokenIds.Contains(t.Id) == false)
                .ToArray();

            var includedTokenIds = includedTokens.Select(t => t.Id).ToArray();

            return includedTokens.Where(t => includedTokenIds.Contains(t.DependsOnId) == false);
        }

        internal IEnumerable<Token> TokensExcluding(IEnumerable<int> tokenIds, CandidateTokenList candidates, IEnumerable<int> excludedRepeatingTokens)
        {
            var candidateIds = candidates.Tokens.Where(t => t.Repeating == false).Select(t => t.Id);

            return TokensExcluding(tokenIds.Concat(candidateIds).Concat(excludedRepeatingTokens));
        }
    }
}
