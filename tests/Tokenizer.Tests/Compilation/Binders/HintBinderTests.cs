using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class HintBinderTests
{
    [Fact]
    public void GivenDefinitionWithHints_WhenBinding_ThenTemplateHasHints()
    {
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", Optional: false));
        definition.Hints.Add(new Hint("receipt", Optional: false));
        var template = new TemplateBuilder().Build();

        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("invoice", template.Hints[0].Text);
        Assert.Equal("receipt", template.Hints[1].Text);
    }

    [Fact]
    public void GivenDefinitionWithDuplicateHints_WhenBinding_ThenDuplicatesAreSkipped()
    {
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", Optional: false));
        definition.Hints.Add(new Hint("invoice", Optional: false));
        var template = new TemplateBuilder().Build();

        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Single(template.Hints);
    }

    [Fact]
    public void GivenDefinitionWithNoHints_WhenBinding_ThenTemplateHasNoHints()
    {
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        HintBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Empty(template.Hints);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenBinding_ThenRecordsHintAddedEvents()
    {
        var definition = new TemplateDefinition();
        definition.Hints.Add(new Hint("invoice", Optional: false));
        var template = new TemplateBuilder().Build();
        var collector = new CompilationDiagnosticCollector();

        HintBinder.Bind(definition, template, collector);

        var diagnostics = collector.GetCompilationResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(CompilationEventType.HintAdded, diagnostics.Events[0].Type);
    }
}
