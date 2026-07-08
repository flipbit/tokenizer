using Tokens.Builders;
using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TagBinderTests
{
    [Fact]
    public void GivenDefinitionWithTags_WhenBinding_ThenTemplateHasTags()
    {
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        definition.Tags.Add("receipt");
        var template = new TemplateBuilder().Build();

        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Equal(2, template.Tags.Count);
        Assert.Equal("invoice", template.Tags[0]);
        Assert.Equal("receipt", template.Tags[1]);
    }

    [Fact]
    public void GivenDefinitionWithDuplicateTags_WhenBinding_ThenDuplicatesAreSkipped()
    {
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        definition.Tags.Add("invoice");
        var template = new TemplateBuilder().Build();

        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Single(template.Tags);
    }

    [Fact]
    public void GivenDefinitionWithNoTags_WhenBinding_ThenTemplateHasNoTags()
    {
        var definition = new TemplateDefinition();
        var template = new TemplateBuilder().Build();

        TagBinder.Bind(definition, template, NullDiagnosticCollector.Instance);

        Assert.Empty(template.Tags);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenBinding_ThenRecordsTagAddedEvents()
    {
        var definition = new TemplateDefinition();
        definition.Tags.Add("invoice");
        var template = new TemplateBuilder().Build();
        var collector = new DiagnosticCollector(inputContent: null);

        TagBinder.Bind(definition, template, collector);

        var diagnostics = collector.GetResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(DiagnosticEventType.TagAdded, diagnostics.Events[0].Type);
    }
}
