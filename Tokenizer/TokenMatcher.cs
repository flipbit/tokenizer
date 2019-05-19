using System;
using System.Collections.Generic;
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
            Templates = new List<Template>();
            tokenizer = new Tokenizer();
            log = LogProvider.GetLogger(typeof(TokenMatcher));
        }

        public TokenMatcher(TokenizerOptions options) : this()
        {
            tokenizer.Options = options;
        }

        public IList<Template> Templates { get; }

        public TokenMatch<T> Match<T>(string input) where T : class, new()
        {
            var results = new TokenMatch<T>();

            foreach (var template in Templates)
            {
                log.Info("Start: Matching: {0}", template.Name);

                try
                {
                    var result = tokenizer.Tokenize<T>(template, input);

                    results.Results.Add(result);
   
                    log.Info("  Match Success: {0}", result.Success);
                    log.Info("  Total Matches: {0}", result.Matches.Count);
                    log.Info("  Total Errors : {0}", result.Exceptions.Count);
                    
                    log.Info("Finish: Matching: {0}", template.Name);
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

            return results;
        }

        public TokenMatcher RegisterTemplate(string content, string name)
        {
            var template = parser.Parse(content, name);

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
    }
}
