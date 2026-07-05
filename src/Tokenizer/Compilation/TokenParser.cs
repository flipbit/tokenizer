using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Compilation;

/// <summary>
/// Parser that converts a string into a <see cref="Template"/> that can be
/// used to extract objects from input strings.
/// </summary>
internal class TokenParser
{
    private readonly List<Type> transformers;
    private readonly List<Type> validators;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    private readonly ILogger<TokenParser> log;

    public TokenizerOptions Options { get; }

    public TokenParser() : this(new TokenizerOptions())
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
        RegisterTransformer<ToIntTransformer>();
        RegisterTransformer<ToDecimalTransformer>();
        RegisterTransformer<ToBooleanTransformer>();
        RegisterTransformer<ToGuidTransformer>();
        RegisterTransformer<TruncateTransformer>();
        RegisterTransformer<DefaultValueTransformer>();
        RegisterTransformer<RegexReplaceTransformer>();
        RegisterTransformer<TitleCaseTransformer>();

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
        RegisterValidator<IsAlphanumericValidator>();
        RegisterValidator<IsIntegerValidator>();
        RegisterValidator<IsGuidValidator>();
        RegisterValidator<IsIpAddressValidator>();
        RegisterValidator<IsInRangeValidator>();
        RegisterValidator<MatchesRegexValidator>();

        // Register custom transformers/validators from options
        foreach (var transformerType in options.Transformers)
        {
            if (!transformers.Contains(transformerType))
            {
                transformers.Add(transformerType);
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Registered custom transformer from options: {TransformerType}", transformerType.Name);
                }
            }
        }

        foreach (var validatorType in options.Validators)
        {
            if (!validators.Contains(validatorType))
            {
                validators.Add(validatorType);
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Registered custom validator from options: {ValidatorType}", validatorType.Name);
                }
            }
        }
    }

    public TokenParser RegisterTransformer<T>() where T : ITokenTransformer
    {
        transformers.Add(typeof(T));

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Registered transformer: {TransformerType}", typeof(T).Name);
        }

        return this;
    }

    public TokenParser RegisterValidator<T>() where T : ITokenValidator
    {
        validators.Add(typeof(T));

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Registered validator: {ValidatorType}", typeof(T).Name);
        }

        return this;
    }

    public Template Parse(TextReader reader)
    {
        var content = reader.ReadToEnd();
        var name = GenerateTemplateName(content);
        return Parse(content, name);
    }

    public Template Parse(TextReader reader, string name)
    {
        var content = reader.ReadToEnd();
        return Parse(content, name);
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

        if (log.IsEnabled(LogLevel.Debug))
        {
            log.LogDebug("Starting template parsing: {TemplateName}, ContentLength: {ContentLength}", name, content.Length);
        }

        if (Options.MaxTemplateLength > 0 && content.Length > Options.MaxTemplateLength)
        {
            throw new ParsingException(
                $"Template length {content.Length:N0} exceeds maximum allowed length of {Options.MaxTemplateLength:N0}. " +
                "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.",
                new Tokens.Enumerators.FileLocation());
        }

        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Start: Parsing Template: {TemplateName}", name);
        }

        try
        {
            var preTemplate = new AstTemplateDefinitionParser().Parse(content, Options);

            var template = new Template(name, preTemplate.Options);

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("AST parsing complete: {TokenCount} tokens found in template {TemplateName}",
                    preTemplate.Tokens.Count, template.Name);
            }

            if (string.IsNullOrWhiteSpace(preTemplate.Name) == false)
            {
                template.Name = preTemplate.Name;
                if (log.IsEnabled(LogLevel.Debug))
                {
                    log.LogDebug("Template name set from front matter: {TemplateName}", template.Name);
                }
            }

            foreach (var hint in preTemplate.Hints)
            {
                if (template.Hints.Any(t => t == hint) == false)
                {
                    template.AddHint(hint);
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug("Added hint to template {TemplateName}: {Hint}", template.Name, hint);
                    }
                }
            }

            foreach (var tag in preTemplate.Tags)
            {
                if (template.Tags.Any(t => t == tag) == false)
                {
                    template.AddTag(tag);
                    if (log.IsEnabled(LogLevel.Debug))
                    {
                        log.LogDebug("Added tag to template {TemplateName}: {Tag}", template.Name, tag);
                    }
                }
            }

            foreach (var preToken in preTemplate.Tokens)
            {
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Parsing token {TokenId}: Name={TokenName}, Content={TokenContent}, Optional={Optional}, Repeating={Repeating}",
                        preToken.Id, preToken.Name ?? "(unnamed)", preToken.Content, preToken.IsOptional, preToken.IsRepeating);
                }

                var preamble = ComputePreamble(preToken, template.Options, log);
                var location = preToken.Location ?? new Enumerators.FileLocation();
                var token = new Token(preToken.Content, preToken.Name ?? string.Empty, preamble, location);

                token.IsOptional = preToken.IsOptional;
                token.IsRepeating = preToken.IsRepeating;
                token.TerminateOnNewLine = preToken.TerminateOnNewLine;
                token.IsRequired = preToken.IsRequired;
                token.DependsOnId = preToken.DependsOnId;
                token.IsFrontMatterToken = preToken.IsFrontMatterToken;
                token.IsNull = preToken.IsNull;
                token.IsSingleUse = preToken.IsSingleUse;

                // All tokens optional if out-of-order enabled
                if (template.Options.OutOfOrderTokens)
                {
                    token.IsOptional = true;
                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Token {TokenId} marked as optional due to OutOfOrderTokens option", token.Id);
                    }
                }

                // Apply global newline termination option from front matter if set
                if (token.TerminateOnNewLine == false && template.Options.TerminateOnNewLine)
                {
                    token.TerminateOnNewLine = true;
                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Token {TokenId} TerminateOnNewLine applied from global option", token.Id);
                    }
                }

                ParseTokenDecorators(preToken, token);

                template.AddToken(token);

                // Link repeating split tokens to their non-repeating counterpart:
                // when the binder splits a Repeating token with a multiline preamble,
                // it produces a non-repeating token followed by a repeating one with
                // the same name. The repeating token should not match until the
                // non-repeating one has been consumed.
                if (token.IsRepeating && token.DependsOnId == -1 && template.Tokens.Count >= 2)
                {
                    var previous = template.Tokens.Last(t => t.Id != token.Id);
                    if (previous.Name == token.Name && previous.IsRepeating == false)
                    {
                        token.DependsOnId = previous.Id;
                        if (log.IsEnabled(LogLevel.Trace))
                        {
                            log.LogTrace("Token {TokenId} ({TokenName}) linked as dependent of token {ParentId}",
                                token.Id, token.Name, previous.Id);
                        }
                    }
                }

                if (string.IsNullOrEmpty(token.Name) == false)
                {
                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Token[{TokenId:000}]: {Token}", token.Id, token);
                    }
                }
            }

            if (Options.MaxTokenCount > 0 && template.Tokens.Count > Options.MaxTokenCount)
            {
                throw new ParsingException(
                    $"Template contains {template.Tokens.Count} tokens, exceeding maximum of {Options.MaxTokenCount:N0}. " +
                    "Increase TokenizerOptions.MaxTokenCount to allow more tokens.",
                    new Tokens.Enumerators.FileLocation());
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Parsed '{TemplateName}' - {ContentLength} byte(s) in {Elapsed}", template.Name, content.Length, stopwatch?.Elapsed.ToString("g"));
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Template parsing complete: {TemplateName}, TotalTokens: {TokenCount}, Duration: {Duration}",
                    template.Name, template.Tokens.Count, stopwatch?.Elapsed.TotalMilliseconds ?? 0);
            }

            return template;
        }
        catch (TokenizerException ex)
        {
            log.LogError(ex, "Template parsing failed for {TemplateName}: {ErrorMessage}, Pattern: {Pattern}",
                name, ex.Message, content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unexpected error during template parsing: {TemplateName}, Pattern: {Pattern}",
                name, content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            throw;
        }
    }

    private void ParseTokenDecorators(TokenDefinition preToken, Token token)
    {
        if (log.IsEnabled(LogLevel.Trace))
        {
            log.LogTrace("Parsing decorators for token {TokenId} ({TokenName}): {DecoratorCount} decorator(s) found",
                preToken.Id, preToken.Name ?? "(unnamed)", preToken.Decorators.Count);
        }

        // If pre-token has value set, add transformer to set it when parsing
        if (string.IsNullOrEmpty(preToken.Value) == false)
        {
            var setContext = new TokenDecoratorContext(typeof(SetTransformer), _decoratorCache);
            setContext.AddParameter(preToken.Value);
            token.AddDecorator(setContext);

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Token {TokenId} ({TokenName}): Added SetTransformer with value: {Value}",
                    preToken.Id, preToken.Name ?? "(unnamed)", preToken.Value);
            }
        }

        foreach (var decorator in preToken.Decorators)
        {
            if (IsConcatenationDecorator(preToken.Name ?? string.Empty, decorator, out var joiningString))
            {
                token.CanConcatenate = true;
                token.ConcatenationString = joiningString;

                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Token {TokenId} ({TokenName}): Applied concatenation decorator with joining string: {JoiningString}",
                        preToken.Id, preToken.Name ?? "(unnamed)", joiningString ?? "(empty)");
                }

                continue;
            }

            TokenDecoratorContext? context = null;

            foreach (var operatorType in transformers)
            {
                if (string.Equals(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (decorator.IsNotDecorator)
                    {
                        log.LogError("Token {TokenId} ({TokenName}): Transformer {TransformerName} cannot be prefixed with '!' character",
                            preToken.Id, preToken.Name ?? "(unnamed)", decorator.Name);
                        throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                    }

                    context = new TokenDecoratorContext(operatorType, _decoratorCache);

                    foreach (var arg in decorator.Args)
                    {
                        context.AddParameter(arg);
                    }

                    token.AddDecorator(context);

                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Token {TokenId} ({TokenName}): Applied transformer {TransformerName} with {ArgCount} argument(s)",
                            preToken.Id, preToken.Name ?? "(unnamed)", operatorType.Name, decorator.Args.Count);
                    }

                    break;
                }
            }

            if (context != null) continue;

            foreach (var validatorType in validators)
            {
                if (string.Equals(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    context = new TokenDecoratorContext(validatorType, _decoratorCache);

                    foreach (var arg in decorator.Args)
                    {
                        context.AddParameter(arg);
                    }

                    context.IsNotValidator = decorator.IsNotDecorator;

                    token.AddDecorator(context);

                    if (log.IsEnabled(LogLevel.Trace))
                    {
                        log.LogTrace("Token {TokenId} ({TokenName}): Applied validator {ValidatorName} with {ArgCount} argument(s), IsNot: {IsNot}",
                            preToken.Id, preToken.Name ?? "(unnamed)", validatorType.Name, decorator.Args.Count, decorator.IsNotDecorator);
                    }

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
                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Token {TokenId} ({TokenName}): Front matter token validation passed",
                        preToken.Id, preToken.Name ?? "(unnamed)");
                }
            }
        }
    }

    private bool IsConcatenationDecorator(string name, DecoratorDefinition decorator, out string? joiningString)
    {
        joiningString = null;

        if (!string.Equals("concat", decorator.Name, StringComparison.InvariantCultureIgnoreCase)) return false;

        if (decorator.Args.Count == 1)
        {
            joiningString = decorator.Args[0];
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Concat decorator detected for token {TokenName} with joining string: {JoiningString}",
                    name ?? "(unnamed)", joiningString);
            }
        }
        else if (decorator.Args.Count == 0)
        {
            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Concat decorator detected for token {TokenName} with no joining string (will use empty string)",
                    name ?? "(unnamed)");
            }
        }

        if (decorator.Args.Count > 1)
        {
            log.LogError("Token {TokenName}: Concat() decorator has {ArgCount} arguments, expected 0 or 1",
                name ?? "(unnamed)", decorator.Args.Count);
            throw new TokenizerException($"Token '{name}' Concat() must have a single argument.");
        }

        return true;

    }

    private string ComputePreamble(Definitions.TokenDefinition preToken, TokenizerOptions options, ILogger log)
    {
        string preamble;

        if (options.TrimLeadingWhitespaceInTokenPreamble)
        {
            if (preToken.Preamble.IsOnlySpaces())
            {
                preamble = preToken.Preamble;
            }
            else if (string.IsNullOrWhiteSpace(preToken.Preamble))
            {
                preamble = preToken.Preamble.TrimLeadingSpaces();
            }
            else
            {
                preamble = preToken.Preamble.TrimStart();
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Token {TokenId} preamble trimmed from {OriginalLength} to {TrimmedLength} characters",
                    preToken.Id, preToken.Preamble.Length, preamble.Length);
            }
        }
        else
        {
            preamble = preToken.Preamble;
        }

        if (options.TrimPreambleBeforeNewLine)
        {
            if (string.IsNullOrEmpty(preamble) == false && preamble.IndexOf('\n') > -1)
            {
                var idx = preamble.LastIndexOf('\n');
                var tail = preamble.Substring(idx + 1);

                if (log.IsEnabled(LogLevel.Trace))
                {
                    log.LogTrace("Token {TokenId} preamble trimmed before last newline: {OriginalLength} to {TrimmedLength} characters",
                        preToken.Id, preamble.Length, tail.Length);
                }

                preamble = tail;
            }
        }

        return preamble;
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
