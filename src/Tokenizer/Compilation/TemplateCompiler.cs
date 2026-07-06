using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Transformers;

namespace Tokens.Compilation;

/// <summary>
/// Compiles template pattern strings into <see cref="Template"/> objects
/// that can be used to extract structured data from input text.
/// </summary>
internal class TemplateCompiler
{
    private static int templateCounter;

    private readonly DecoratorRegistry registry;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    private readonly ILogger<TemplateCompiler> log;

    public TokenizerOptions Options { get; }

    public TemplateCompiler(TokenizerOptions options, ILogger<TemplateCompiler>? logger = null)
    {
        log = logger ?? NullLogger<TemplateCompiler>.Instance;

        Options = options;
        registry = new DecoratorRegistry(options);
    }

    public Template Compile(string content)
    {
        Stopwatch? stopwatch = null;

        if (log.IsEnabled(LogLevel.Trace))
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        if (Options.MaxTemplateLength > 0 && content.Length > Options.MaxTemplateLength)
        {
            throw new ParsingException(
                $"Template length {content.Length:N0} exceeds maximum allowed length of {Options.MaxTemplateLength:N0}. " +
                "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.",
                new Tokens.Enumerators.FileLocation());
        }

        try
        {
            var preTemplate = new AstTemplateDefinitionParser().Parse(content, Options);

            var name = string.IsNullOrWhiteSpace(preTemplate.Name)
                ? $"Template_{Interlocked.Increment(ref templateCounter)}"
                : preTemplate.Name;

            var template = new Template(content, name, preTemplate.Options);

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Starting template compilation: {TemplateName}, ContentLength: {ContentLength}", template.Name, content.Length);
            }

            if (log.IsEnabled(LogLevel.Trace))
            {
                log.LogTrace("Start: Compiling Template: {TemplateName}", template.Name);
            }

            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("AST parsing complete: {TokenCount} tokens found in template {TemplateName}",
                    preTemplate.Tokens.Count, template.Name);
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
            log.LogError(ex, "Template compilation failed: {ErrorMessage}, Pattern: {Pattern}",
                ex.Message, content.Length > 200 ? content.Substring(0, 200) + "..." : content);
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unexpected error during template compilation: Pattern: {Pattern}",
                content.Length > 200 ? content.Substring(0, 200) + "..." : content);
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

            foreach (var operatorType in registry.Transformers)
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

            foreach (var validatorType in registry.Validators)
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
}
