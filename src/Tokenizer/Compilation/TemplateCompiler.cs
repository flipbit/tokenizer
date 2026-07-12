using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Compilation.Binders;
using Tokens.Compilation.Parsing;
using Tokens.Diagnostics;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation;

/// <summary>
/// Compiles template pattern strings into <see cref="Template"/> objects
/// that can be used to extract structured data from input text.
/// </summary>
internal sealed class TemplateCompiler
{
    private readonly DecoratorRegistry _registry;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();
    private readonly ILogger<TemplateCompiler> _log;

    public TokenizerOptions Options { get; }

    public TemplateCompiler(TokenizerOptions options, ILoggerFactory? loggerFactory = null)
    {
        Options = options;
        _registry = new DecoratorRegistry(options);
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TemplateCompiler>();
    }

    public CompilationResult Compile(string content)
    {
        ICompilationDiagnosticCollector collector = Options.EnableDiagnostics
            ? new CompilationDiagnosticCollector()
            : NullCompilationDiagnosticCollector.Instance;

        TemplateLengthValidator.Validate(content, Options);

        _log.LogDebug("Starting template compilation (content length: {ContentLength})", content.Length);

        try
        {
            var definition = new AstTemplateDefinitionParser().Parse(content, Options);
            var id = content.ComputeHash();
            var template = TemplateFactory.Create(id, definition);

            HintBinder.Bind(definition, template, collector);
            TagBinder.Bind(definition, template, collector);
            TokenBinder.Bind(definition, template, _registry, _decoratorCache, collector);
            TokenCountValidator.Validate(template, Options);

            if (collector.IsEnabled)
            {
                collector.Record(CompilationEventType.CompilationCompleted,
                    detail: $"Template '{template.Name}' compiled with {template.Tokens.Count} token(s)");
            }

            _log.LogDebug("Template '{TemplateName}' compiled successfully with {TokenCount} token(s)",
                template.Name, template.Tokens.Count);

            return new CompilationResult(template, collector.GetResult());
        }
        catch (TokenizerException ex)
        {
            _log.LogError(ex, "Template compilation failed: {Message}", ex.Message);
            ex.Data["CompilationDiagnostics"] = collector.GetResult();
            throw;
        }
        // Intentional catch-all: compilation boundary that wraps unexpected exceptions
        // (after TokenizerException is already caught above) into TokenizerException
        // with diagnostic context attached.
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _log.LogError(ex, "Unexpected error during template compilation: {Message}", ex.Message);
            var wrapped = new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
            wrapped.Data["CompilationDiagnostics"] = collector.GetResult();
            throw wrapped;
        }
    }
}
