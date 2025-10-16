using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tokens.Compilation;
using Tokens.Enumerators;
using Tokens.Logging;
using Tokens.Tokenization;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Class that creates objects and populates their properties with values
    /// from input strings
    /// </summary>
    public class Tokenizer
    {
        private readonly TokenParser parser;
        private readonly ILog log;
        private readonly ITokenizationEngine tokenizationEngine;
        private readonly IHintProcessor hintProcessor;
        private readonly IResultBuilder resultBuilder;

        /// <summary>Gets or sets the options.</summary>
        public TokenizerOptions Options { get; set; }

        /// <summary>Initializes a new instance of the <see cref="Tokenizer"/> class.</summary>
        public Tokenizer() : this(TokenizerOptions.Defaults) { }

        public Tokenizer(TokenizerOptions options)
        {
            parser = new TokenParser(options);

            Options = options;
            log = LogProvider.For<Tokenizer>();
            
            // Create service instances
            tokenizationEngine = new TokenizationEngine();
            hintProcessor = new HintProcessor();
            resultBuilder = new ResultBuilder();
        }

        public TokenizeResult Tokenize(string template, string input)
        {
            var t = parser.Parse(template);

            return Tokenize(t, input);
        }

        public TokenizeResult Tokenize(Template template, string input)
        {
            var result = new TokenizeResult(template);

            Tokenize(result, null, template, input);

            return result;

        }

        public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
        {
            var template = parser.Parse(pattern);

            return Tokenize<T>(template, input);
        }

        public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
        {
            var result = new TokenizeResult<T>(template);

            Tokenize(result, result.Value, template, input);

            return result;
        }

        private void Tokenize(TokenizeResultBase result, object value, Template template, string input)
        {
            log.Verbose($"Start: Processing: {template.Name}");

            using (new LogIndentation())
            {
                // Create and initialize the tokenization context
                using (var context = new TokenizationContext())
                {
                    context.Initialize(input);

                    // Process hints first
                    var hintsMissing = hintProcessor.FindAndValidateHints(template, context.Enumerator, result);

                    // Only proceed with tokenization if hints are not missing
                    if (!hintsMissing)
                    {
                        // Process the main tokenization using the engine
                        tokenizationEngine.ProcessTokenization(template, input, value, context, result);
                    }

                    // Build unmatched tokens collection
                    resultBuilder.BuildUnmatchedTokens(template, result);

                    log.Verbose($"Found {result.Tokens.Matches.Count} matches.");
                    log.Verbose("{0} required tokens were missing.", result.Tokens.Misses.Count(t => t.Required));
                }
            }

            log.Verbose($"Finished: Processing: {template.Name}");
        }

        public Tokenizer RegisterTransformer<T>() where T : ITokenTransformer
        {
            parser.RegisterTransformer<T>();

            return this;
        }

        public Tokenizer RegisterValidator<T>() where T : ITokenValidator
        {
            parser.RegisterValidator<T>();

            return this;
        }

    }
}
