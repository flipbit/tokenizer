using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Compilation
{
    /// <summary>
    /// Parser that converts a string into a <see cref="Template"/> that can be
    /// used to extract objects from input strings.
    /// </summary>
    internal class TokenParser
    {
        private readonly List<Type> transformers;
        private readonly List<Type> validators;

        private readonly ILogger<TokenParser> log;

        public TokenizerOptions Options { get; set; }

        public TokenParser() : this(TokenizerOptions.Defaults)
        {
        }

        public TokenParser(TokenizerOptions options) : this(options, null)
        {
        }

        public TokenParser(TokenizerOptions options, ILogger<TokenParser>? logger)
        {
            log = logger ?? NullLogger<TokenParser>.Instance;

            Options = options;

            transformers = new List<Type>();
            validators = new List<Type>();

            // Add default transformers/validators
            RegisterTransformer<ToDateTimeTransformer>();
            RegisterTransformer<ToDateTimeUtcTransformer>();
            RegisterTransformer<ToLowerTransformer>();
            RegisterTransformer<ToUpperTransformer>();
            RegisterTransformer<TrimTransformer>();
            RegisterTransformer<SubstringAfterTransformer>();
            RegisterTransformer<SubstringBeforeTransformer>();
            RegisterTransformer<SetTransformer>();
            RegisterTransformer<ReplaceTransformer>();
            RegisterTransformer<RemoveTransformer>();
            RegisterTransformer<SubstringAfterLastTransformer>();
            RegisterTransformer<SubstringBeforeLastTransformer>();
            RegisterTransformer<RemoveEndTransformer>();
            RegisterTransformer<RemoveStartTransformer>();
            RegisterTransformer<SplitTransformer>();

            RegisterValidator<IsNumericValidator>();
            RegisterValidator<MaxLengthValidator>();
            RegisterValidator<MinLengthValidator>();
            RegisterValidator<IsDomainNameValidator>();
            RegisterValidator<IsPhoneNumberValidator>();
            RegisterValidator<IsEmailValidator>();
            RegisterValidator<IsUrlValidator>();
            RegisterValidator<IsLooseUrlValidator>();
            RegisterValidator<IsLooseAbsoluteUrlValidator>();
            RegisterValidator<IsDateTimeValidator>();
            RegisterValidator<IsNotEmptyValidator>();
            RegisterValidator<IsNotValidator>();
            RegisterValidator<StartsWithValidator>();
            RegisterValidator<EndsWithValidator>();
            RegisterValidator<ContainsValidator>();
        }

        public TokenParser RegisterTransformer<T>() where T : ITokenTransformer
        {
            transformers.Add(typeof(T));

            log.LogDebug("Registered transformer: {TransformerType}", typeof(T).Name);

            return this;
        }

        public TokenParser RegisterValidator<T>() where T : ITokenValidator
        {
            validators.Add(typeof(T));

            log.LogDebug("Registered validator: {ValidatorType}", typeof(T).Name);

            return this;
        }

        public Template Parse(string content)
        {
            var name = GenerateTemplateName(content);

            return Parse(content, name);
        }

        public Template Parse(string content, string name)
        {
            Stopwatch? stopwatch = null;

            if (log.IsEnabled(LogLevel.Trace))
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            log.LogInformation("Starting template parsing: {TemplateName}, ContentLength: {ContentLength}", name, content.Length);

            var template = new Template(name, content);

            log.LogTrace("Start: Parsing Template: {TemplateName}", template.Name);

            try
            {
                var preTemplate = new AstTemplateDefinitionParser().Parse(content, Options);

                template.Options = preTemplate.Options;

                log.LogDebug("AST parsing complete: {TokenCount} tokens found in template {TemplateName}",
                    preTemplate.Tokens.Count, template.Name);

            if (string.IsNullOrWhiteSpace(preTemplate.Name) == false)
            {
                template.Name = preTemplate.Name;
                log.LogDebug("Template name set from front matter: {TemplateName}", template.Name);
            }

            foreach (var hint in preTemplate.Hints)
            {
                if (template.Hints.Any(t => t == hint) == false)
                {
                    template.Hints.Add(hint);
                    log.LogDebug("Added hint to template {TemplateName}: {Hint}", template.Name, hint);
                }
            }

            foreach (var tag in preTemplate.Tags)
            {
                if (template.Tags.Any(t => t == tag) == false)
                {
                    template.Tags.Add(tag);
                    log.LogDebug("Added tag to template {TemplateName}: {Tag}", template.Name, tag);
                }
            }

            foreach (var preToken in preTemplate.Tokens)
            {
                log.LogTrace("Parsing token {TokenId}: Name={TokenName}, Content={TokenContent}, Optional={Optional}, Repeating={Repeating}",
                    preToken.Id, preToken.Name ?? "(unnamed)", preToken.Content, preToken.Optional, preToken.Repeating);

                var token = new Token(preToken.Content);

                if (Options.TrimLeadingWhitespaceInTokenPreamble)
                {
                    if (preToken.Preamble.IsOnlySpaces())
                    {
                        token.Preamble = preToken.Preamble;
                    }
                    else if (string.IsNullOrWhiteSpace(preToken.Preamble))
                    {
                        token.Preamble = preToken.Preamble.TrimLeadingSpaces();
                    }
                    else
                    {
                        token.Preamble = preToken.Preamble.TrimStart();
                    }

                    log.LogTrace("Token {TokenId} preamble trimmed from {OriginalLength} to {TrimmedLength} characters",
                        preToken.Id, preToken.Preamble.Length, token.Preamble.Length);
                }
                else
                {
                    token.Preamble = preToken.Preamble;
                }

                // New behavior: if TrimPreambleBeforeNewLine is enabled (from options or front matter),
                // then trim any content before the last newline in the preamble. This aligns AST pipeline
                // with legacy TemplateDefinitionParser behavior.
                if (template.Options.TrimPreambleBeforeNewLine)
                {
                    var pre = token.Preamble;
                    if (string.IsNullOrEmpty(pre) == false && pre.IndexOf('\n') > -1)
                    {
                        var idx = pre.LastIndexOf('\n');
                        var tail = pre.Substring(idx + 1);
                        token.Preamble = tail;

                        log.LogTrace("Token {TokenId} preamble trimmed before last newline: {OriginalLength} to {TrimmedLength} characters",
                            preToken.Id, pre.Length, tail.Length);
                    }
                }

                token.Name = preToken.Name;
                token.Optional = preToken.Optional;
                token.Repeating = preToken.Repeating;
                token.TerminateOnNewLine = preToken.TerminateOnNewline;
                token.Required = preToken.Required;
                token.Id = preToken.Id;
                token.DependsOnId = preToken.DependsOnId;
                token.IsFrontMatterToken = preToken.IsFrontMatterToken;
                token.IsNull = preToken.IsNull;
                token.Location = preToken.Location;
                token.ConsiderOnce = preToken.ConsiderOnce;

                // All tokens optional if out-of-order enabled
                if (template.Options.OutOfOrderTokens)
                {
                    token.Optional = true;
                    log.LogTrace("Token {TokenId} marked as optional due to OutOfOrderTokens option", token.Id);
                }

                // Apply global newline termination option from front matter if set
                if (token.TerminateOnNewLine == false && template.Options.TerminateOnNewline)
                {
                    token.TerminateOnNewLine = true;
                    log.LogTrace("Token {TokenId} TerminateOnNewLine applied from global option", token.Id);
                }

                ParseTokenDecorators(preToken, token);

                template.AddToken(token);

                if (string.IsNullOrEmpty(token.Name) == false)
                {
                    log.LogTrace("Token[{TokenId:000}]: {Token}", token.Id, token);
                }
            }

            log.LogTrace("Parsed '{TemplateName}' - {ContentLength} byte(s) in {Elapsed}", template.Name, content.Length, stopwatch?.Elapsed.ToString("g"));

            log.LogInformation("Template parsing complete: {TemplateName}, TotalTokens: {TokenCount}, Duration: {Duration}",
                template.Name, template.Tokens.Count, stopwatch?.Elapsed.TotalMilliseconds ?? 0);

            return template;
            }
            catch (TokenizerException ex)
            {
                log.LogError(ex, "Template parsing failed for {TemplateName}: {ErrorMessage}, Pattern: {Pattern}",
                    name, ex.Message, content);
                throw;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected error during template parsing: {TemplateName}, Pattern: {Pattern}",
                    name, content);
                throw;
            }
        }

        private void ParseTokenDecorators(TokenDefinition preToken, Token token)
        {
            log.LogTrace("Parsing decorators for token {TokenId} ({TokenName}): {DecoratorCount} decorator(s) found",
                preToken.Id, preToken.Name ?? "(unnamed)", preToken.Decorators.Count);

            // If pre-token has value set, add transformer to set it when parsing
            if (string.IsNullOrEmpty(preToken.Value) == false)
            {
                var setContext = new TokenDecoratorContext(typeof(SetTransformer));
                setContext.Parameters.Add(preToken.Value);
                token.Decorators.Add(setContext);

                log.LogTrace("Token {TokenId} ({TokenName}): Added SetTransformer with value: {Value}",
                    preToken.Id, preToken.Name ?? "(unnamed)", preToken.Value);
            }

            foreach (var decorator in preToken.Decorators)
            {
                if (IsConcatenationDecorator(preToken.Name, decorator, out var joiningString))
                {
                    token.Concatenate = true;
                    token.ConcatenationString = joiningString;

                    log.LogTrace("Token {TokenId} ({TokenName}): Applied concatenation decorator with joining string: {JoiningString}",
                        preToken.Id, preToken.Name ?? "(unnamed)", joiningString ?? "(empty)");

                    continue;
                }

                TokenDecoratorContext context = null;

                foreach (var operatorType in transformers)
                {
                    if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (decorator.IsNotDecorator)
                        {
                            log.LogError("Token {TokenId} ({TokenName}): Transformer {TransformerName} cannot be prefixed with '!' character",
                                preToken.Id, preToken.Name ?? "(unnamed)", decorator.Name);
                            throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                        }

                        context = new TokenDecoratorContext(operatorType);

                        foreach (var arg in decorator.Args)
                        {
                            context.Parameters.Add(arg);
                        }

                        token.Decorators.Add(context);

                        log.LogTrace("Token {TokenId} ({TokenName}): Applied transformer {TransformerName} with {ArgCount} argument(s)",
                            preToken.Id, preToken.Name ?? "(unnamed)", operatorType.Name, decorator.Args.Count);

                        break;
                    }
                }

                if (context != null) continue;

                foreach (var validatorType in validators)
                {
                    if (string.Compare(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        context = new TokenDecoratorContext(validatorType);

                        foreach (var arg in decorator.Args)
                        {
                            context.Parameters.Add(arg);
                        }

                        context.IsNotValidator = decorator.IsNotDecorator;

                        token.Decorators.Add(context);

                        log.LogTrace("Token {TokenId} ({TokenName}): Applied validator {ValidatorName} with {ArgCount} argument(s), IsNot: {IsNot}",
                            preToken.Id, preToken.Name ?? "(unnamed)", validatorType.Name, decorator.Args.Count, decorator.IsNotDecorator);

                        break;
                    }
                }

                if (context == null)
                {
                    log.LogError("Token {TokenId} ({TokenName}): Unknown decorator/operation: {DecoratorName}",
                        preToken.Id, preToken.Name ?? "(unnamed)", decorator.Name);
                    throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
                }
            }

            if (preToken.IsFrontMatterToken)
            {
                var hasSetTransformer = token.Decorators.Any(d => d.DecoratorType == typeof(SetTransformer));

                if (hasSetTransformer == false)
                {
                    log.LogError("Token {TokenId} ({TokenName}): Front matter token missing required assignment operation",
                        preToken.Id, preToken.Name ?? "(unnamed)");
                    throw new TokenizerException($"Front Matter Token '{preToken.Name}' must have an assignment operation.");
                }
                else
                {
                    log.LogTrace("Token {TokenId} ({TokenName}): Front matter token validation passed",
                        preToken.Id, preToken.Name ?? "(unnamed)");
                }
            }
        }

        private bool IsConcatenationDecorator(string name, DecoratorDefinition decorator, out string joiningString)
        {
            joiningString = null;

            if (string.Compare("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase) != 0) return false;

            if (decorator.Args.Count == 1)
            {
                joiningString = decorator.Args[0];
                log.LogTrace("Concat decorator detected for token {TokenName} with joining string: {JoiningString}",
                    name ?? "(unnamed)", joiningString);
            }
            else if (decorator.Args.Count == 0)
            {
                log.LogTrace("Concat decorator detected for token {TokenName} with no joining string (will use empty string)",
                    name ?? "(unnamed)");
            }

            if (decorator.Args.Count > 1)
            {
                log.LogError("Token {TokenName}: Concat() decorator has {ArgCount} arguments, expected 0 or 1",
                    name ?? "(unnamed)", decorator.Args.Count);
                throw new TokenizerException($"Token '{name}' Concat() must have a single argument.");
            }

            return true;

        }

        private string GenerateTemplateName(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "(empty)";

            var name = new StringBuilder();

            var words = 0;
            var lastCharWasANewLine = false;

            var startIndex = 0;
            var hasFrontmatter = content.StartsWith("---\n") || content.StartsWith("---\r\n");

            if (hasFrontmatter)
            {
                var frontmatterEndIndex = content.IndexOf("\n---", 5);

                if (frontmatterEndIndex > -1) startIndex = frontmatterEndIndex + 4;
            }

            for (var i = startIndex; i < content.Length; i++)
            {
                var c = content[i];
                


                if (char.IsWhiteSpace(c))
                {
                    if (lastCharWasANewLine) continue;
                    if (name.Length == 0) continue;

                    lastCharWasANewLine = true;

                    words++;
                    if (words <= 2)
                    {
                        name.Append(' ');
                        continue;
                    }

                    name.Append("...");
                    break;
                }

                name.Append(c);
                lastCharWasANewLine = false;
            }

            return name.ToString();
        }
    }
}
