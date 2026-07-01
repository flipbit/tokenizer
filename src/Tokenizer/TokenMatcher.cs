using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation;
using Tokens.Exceptions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Matcher class that can hold multiple <see cref="Template"/> objects, and use
    /// the best match to populate an object from an input string.
    /// </summary>
    public sealed class TokenMatcher
    {
        private readonly Tokenizer tokenizer;
        private readonly TokenParser parser;
        private readonly ILogger<TokenMatcher> log;

        public TokenMatcher() : this(TokenizerOptions.Defaults, (ILoggerFactory?)null)
        {
        }

        public TokenMatcher(TokenizerOptions options) : this(options, (ILoggerFactory?)null)
        {
        }

        public TokenMatcher(TokenizerOptions options, ILoggerFactory? loggerFactory)
        {
            loggerFactory ??= NullLoggerFactory.Instance;

            log = loggerFactory.CreateLogger<TokenMatcher>();
            parser = new TokenParser(options, loggerFactory.CreateLogger<TokenParser>());
            Templates = new TemplateCollection();
            tokenizer = Tokenizer.Create(options, loggerFactory);
        }

        public TemplateCollection Templates { get; }

        public TokenMatcherResult Match(string input)
        {
            return Match(input, null);
        }

        public TokenMatcherResult Match(string input, string[]? tags)
        {
            if (tags == null) tags = new string[0];

            var results = new TokenMatcherResult();

            foreach (var name in Templates.Names)
            {
                if (!Templates.TryGet(name, out var template)) continue;

                log.LogTrace("Start: Matching: {TemplateName}", template.Name);

                // Check template has tags
                if (CheckTemplateTags(template, tags) == false)
                {
                    continue;
                }

                try
                {
                    var result = tokenizer.Tokenize(template, input);

                    results.AddResult(result);

                    log.LogTrace("Match Success: {Success}", result.Success);
                    log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                    log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);

                }
                catch (Exception e)
                {
                    var exception = new TokenMatcherException(e.Message, template, e);

                    log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                    throw exception;
                }

                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
            }

            // Assign best match
            results.BestMatch = results.GetBestMatch();

            return results;

        }

        public TokenMatcherResult<T> Match<T>(string input) where T : class, new()
        {
            return Match<T>(input, null);
        }

        public TokenMatcherResult<T> Match<T>(string input, string[]? tags) where T : class, new()
        {
            if (tags == null) tags = new string[0];

            var results = new TokenMatcherResult<T>();

            foreach (var name in Templates.Names)
            {
                if (!Templates.TryGet(name, out var template)) continue;

                log.LogTrace("Start: Matching: {TemplateName}", template.Name);

                // Check template has tags
                if (CheckTemplateTags(template, tags) == false)
                {
                    continue;
                }

                try
                {
                    var result = tokenizer.Tokenize<T>(template, input);

                    results.AddResult(result);

                    log.LogTrace("Match Success: {Success}", result.Success);
                    log.LogTrace("Total Matches: {MatchCount}", result.Tokens.Matches.Count);
                    log.LogTrace("Total Errors : {ErrorCount}", result.Exceptions.Count);

                }
                catch (Exception e)
                {
                    var exception = new TokenMatcherException(e.Message, template, e);

                    log.LogError(e, "Error processing template: {TemplateName}", template.Name);

                    throw exception;
                }

                log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
            }

            // Assign best match
            results.BestMatch = results.GetBestMatch();

            return results;
        }

        public TokenMatcher RegisterTemplate(string content, string name)
        {
            var template = parser.Parse(content, name);

            Templates.Add(template);

            return this;
        }

        public TokenMatcher RegisterTemplate(string content)
        {
            var template = parser.Parse(content);

            Templates.Add(template);

            return this;
        }

        public TokenMatcher RegisterTransformer<T>() where T : ITokenTransformer
        {
            parser.RegisterTransformer<T>();

            return this;
        }

        public TokenMatcher RegisterValidator<T>() where T : ITokenValidator
        {
            parser.RegisterValidator<T>();

            return this;
        }

        private bool CheckTemplateTags(Template template, string[] tags)
        {
            // No tags specified, always match template
            if (tags.Length == 0) return true;

            // Check template has tags
            if (template.Tags.Any())
            {
                if (template.HasTags(tags, out var missing) == false)
                {
                    log.LogTrace("No tags matching: {MissingTags}", missing);
                    log.LogTrace("Finish: Matching: {TemplateName}", template.Name);
                    return false;
                }

                log.LogTrace("Found tag matching: {Tags}", string.Join(", ", tags));
                return true;
            }

            return false;
        }
    }
}
