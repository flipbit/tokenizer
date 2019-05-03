using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Exceptions;
using Tokens.Logging;
using Tokens.Parsers;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    public class TokenMatcher
    {
        private readonly Tokenizer tokenizer;
        private readonly TokenParser parser;
        private readonly List<Template> templates;
        private readonly ILog log;

        public TokenMatcher()
        {
            parser = new TokenParser();
            templates = new List<Template>();
            tokenizer = new Tokenizer();
            log = LogProvider.GetLogger(typeof(TokenMatcher));
        }

        public TokenMatcher(TokenizerOptions options) : this()
        {
            tokenizer.Options = options;
        }

        public void AddPattern(string pattern)
        {
            AddPattern(pattern, string.Empty);
        }

        public void AddPattern(string pattern, string name)
        {
            var template = parser.Parse(pattern, name);

            templates.Add(template);
        }

        public void ClearPatterns()
        {
            templates.Clear();
        }

        public T Match<T>(string input) where T : class, new()
        {
            return TryMatch<T>(input, out var match) ? match.Result : null;
        }

        public bool TryMatch<T>(string input, out TokenMatch<T> match) where T : class, new()
        {
            var results = new List<TokenMatch<T>>();

            foreach (var template in templates)
            {
                log.Info("Matching: {0}", template.Name);
                try
                {
                    if (tokenizer.TryParse<T>(template, input, out var count, out var result))
                    {
                        results.Add(new TokenMatch<T>
                        {
                            Matches = count,
                            Result = result,
                            Template = template
                        });
                    }
                }
                catch (Exception e)
                {
                    var exception = new TokenMatcherException(e.Message, e)
                    {
                        Template = template
                    };

                    throw exception;
                }
            }

            match = results.OrderByDescending(r => r.Matches).FirstOrDefault();

            return match != null;
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
