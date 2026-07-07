using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tokens.Tokenization;

public class TokenMatchRouterTests
{
    [Fact]
    public void GivenNoMatchInInput_WhenRouteNext_ThenAccumulatesCharacter()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, hintStrategy: null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("x"));
        context.Enumerator.FillBuffer();

        // Act
        router.RouteNext(context);

        // Assert — character accumulated in replacement buffer
        Assert.Equal("x", context.Replacement.ToString());
    }

    [Fact]
    public void GivenMatchingPreamble_WhenRouteNextWithNoCandidates_ThenSetsUpFirstMatch()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, hintStrategy: null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));
        context.Enumerator.FillBuffer();

        // Act
        router.RouteNext(context);

        // Assert — candidates should now be set
        Assert.True(context.Candidates.HasCandidates);
    }

    [Fact]
    public void GivenSecondTokenMatch_WhenRouteNextWithExistingValue_ThenSwitchesTokens()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("A:{First}B:{Second}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, hintStrategy: null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("A:helloB:world"));
        context.Enumerator.FillBuffer();

        // Route through until the first token gets assigned via token switch
        while (!context.Enumerator.IsEmpty && result.Tokens.Matches.Count == 0)
        {
            router.RouteNext(context);
        }

        // Assert — first token should have been assigned
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenRouteNextOnNewline_ThenAssignsAndClearsCandidates()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name$}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);
        var router = new TokenMatchRouter(template, processor,
            NullDiagnosticCollector.Instance, hintStrategy: null);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice\nOther: stuff"));
        context.Enumerator.FillBuffer();

        // Route until we hit the newline
        while (!context.Enumerator.IsEmpty && result.Tokens.Matches.Count == 0)
        {
            router.RouteNext(context);
        }

        // Assert — token should be assigned via newline path
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value.ToString());
    }
}
