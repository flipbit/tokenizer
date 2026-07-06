using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class RepeatingTokenLinkerTests
{
    [Fact]
    public void GivenRepeatingTokenAfterNonRepeatingWithSameName_WhenLinking_ThenDependsOnIdIsSet()
    {
        var nonRepeating = new Token("{Name}", "Name", "Preamble\n", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        Assert.Equal(nonRepeating.Id, repeating.DependsOnId);
    }

    [Fact]
    public void GivenRepeatingTokenWithDifferentNameFromPrevious_WhenLinking_ThenDependsOnIdUnchanged()
    {
        var nonRepeating = new Token("{Other}", "Other", "Preamble", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        Assert.Equal(-1, repeating.DependsOnId);
    }

    [Fact]
    public void GivenNonRepeatingToken_WhenLinking_ThenNothingHappens()
    {
        var token = new Token("{Name}", "Name", "Preamble", new FileLocation());
        token.IsRepeating = false;
        var template = new TemplateBuilder()
            .WithTokens(token)
            .Build();

        RepeatingTokenLinker.Link(token, template, NullDiagnosticCollector.Instance);

        Assert.Equal(-1, token.DependsOnId);
    }

    [Fact]
    public void GivenRepeatingTokenAlreadyLinked_WhenLinking_ThenDependsOnIdUnchanged()
    {
        var nonRepeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        repeating.DependsOnId = 99;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();

        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        Assert.Equal(99, repeating.DependsOnId);
    }

    [Fact]
    public void GivenOnlyOneTokenInTemplate_WhenLinking_ThenNothingHappens()
    {
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(repeating)
            .Build();

        RepeatingTokenLinker.Link(repeating, template, NullDiagnosticCollector.Instance);

        Assert.Equal(-1, repeating.DependsOnId);
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenLinkingSucceeds_ThenRecordsEvent()
    {
        var nonRepeating = new Token("{Name}", "Name", "Preamble\n", new FileLocation());
        var repeating = new Token("{Name}", "Name", "Preamble", new FileLocation());
        repeating.IsRepeating = true;
        var template = new TemplateBuilder()
            .WithTokens(nonRepeating, repeating)
            .Build();
        var collector = new DiagnosticCollector(inputContent: null);

        RepeatingTokenLinker.Link(repeating, template, collector);

        var diagnostics = collector.GetResult()!;
        Assert.Single(diagnostics.Events);
        Assert.Equal(DiagnosticEventType.RepeatingTokenLinked, diagnostics.Events[0].Type);
    }
}
