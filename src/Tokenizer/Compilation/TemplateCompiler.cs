using System.Collections.Concurrent;
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
internal class TemplateCompiler
{
    private readonly DecoratorRegistry _registry;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();

    public TokenizerOptions Options { get; }

    public TemplateCompiler(TokenizerOptions options)
    {
        Options = options;
        _registry = new DecoratorRegistry(options);
    }

    public CompilationResult Compile(string content)
    {
        IDiagnosticCollector collector = Options.EnableDiagnostics
            ? new DiagnosticCollector(inputContent: null)
            : NullDiagnosticCollector.Instance;

        TemplateLengthValidator.Validate(content, Options);

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
                collector.Record(DiagnosticEventType.CompilationCompleted,
                    detail: $"Template '{template.Name}' compiled with {template.Tokens.Count} token(s)");
            }

            return new CompilationResult(template, collector.GetResult());
        }
        catch (TokenizerException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
        }
    }
}
