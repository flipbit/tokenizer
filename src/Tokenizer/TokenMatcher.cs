using System;
using System.Linq;
using Tokens.Exceptions;
using Tokens.Logging;
using Tokens.Parsers;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Matcher class that can hold multiple <see cref="Template"/> objects, and use
    /// the best match to populate an object from an input string.
    /// </summary>
    public class TokenMatcher
    {
        private readonly Tokenizer tokenizer;
        private readonly TokenParser parser;
        private readonly ILog log;

        public TokenMatcher()
        {
            parser = new TokenParser();
            Templates = new TemplateCollection();
            tokenizer = new Tokenizer();
            log = LogProvider.GetLogger(typeof(TokenMatcher));
        }

        public TokenMatcher(TokenizerOptions options) : this()
        {
            tokenizer.Options = options;
        }

        public TemplateCollection Templates { get; }

        public TokenMatcherResult Match(string input)
        {
            return Match(input, null);
        }

        public TokenMatcherResult Match(string input, string[] tags)
        {
            if (tags == null) tags = new string[0];

            var results = new TokenMatcherResult();

            foreach (var name in Templates.Names)
            {
                if (!Templates.TryGet(name, out var template)) continue;

                log.Verbose("Start: Matching: {0}", template.Name);

                using (new LogIndentation())
                {
                    // Check template has tags
                    if (CheckTemplateTags(template, tags) == false)
                    {
                        continue;
                    }

                    try
                    {
                        TokenizeResult result;

                        using (new LogIndentation())
                        {
                            result = tokenizer.Tokenize(template, input);
                        }

                        results.Results.Add(result);

                        log.Verbose("Match Success: {0}", result.Success);
                        log.Verbose("Total Matches: {0}", result.Tokens.Matches.Count);
                        log.Verbose("Total Errors : {0}", result.Exceptions.Count);

                    }
                    catch (Exception e)
                    {
                        var exception = new TokenMatcherException(e.Message, e)
                        {
                            Template = template
                        };

                        log.ErrorException($"Error processing template: {template.Name}", e);

                        throw exception;
                    }
                }

                log.Verbose("Finish: Matching: {0}", template.Name);
            }

            // Assign best match
            results.BestMatch = results.GetBestMatch();

            return results;

        }

        public TokenMatcherResult<T> Match<T>(string input) where T : class, new()
        {
            return Match<T>(input, null);
        }

        public TokenMatcherResult<T> Match<T>(string input, string[] tags) where T : class, new()
        {
            if (tags == null) tags = new string[0];

            var results = new TokenMatcherResult<T>();

            foreach (var name in Templates.Names)
            {
                if (!Templates.TryGet(name, out var template)) continue;

                log.Verbose("Start: Matching: {0}", template.Name);

                using (new LogIndentation())
                {
                    // Check template has tags
                    if (CheckTemplateTags(template, tags) == false)
                    {
                        continue;
                    }

                    try
                    {
                        TokenizeResult<T> result;

                        using (new LogIndentation())
                        {
                            result = tokenizer.Tokenize<T>(template, input);
                        }

                        results.Results.Add(result);

                        log.Verbose("Match Success: {0}", result.Success);
                        log.Verbose("Total Matches: {0}", result.Tokens.Matches.Count);
                        log.Verbose("Total Errors : {0}", result.Exceptions.Count);

                    }
                    catch (Exception e)
                    {
                        var exception = new TokenMatcherException(e.Message, e)
                        {
                            Template = template
                        };

                        log.ErrorException($"Error processing template: {template.Name}", e);

                        throw exception;
                    }
                }

                log.Verbose("Finish: Matching: {0}", template.Name);
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
                    log.Verbose("No tags matching: {0}", missing);
                    log.Verbose("Finish: Matching: {0}", template.Name);
                    return false;
                }
                
                log.Verbose("Found tag matching: {0}", string.Join(", ", tags));
                return true;
            }

            return false;
        }
    }
}
