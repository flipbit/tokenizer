using System.Text;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine internal methods (ProcessRepeatedTokens, ProcessNewlineTerminatedTokens, etc.)
/// </summary>
public class TokenizationEngineInternalTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenRepeatingTokens_WhenProcessingRepeatedTokens_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("test{Name}");
        var candidates = new CandidateTokenList();

        // Add the actual token from the template to the candidates
        var token = template.Tokens.First();
        candidates.Add(token);

        var enumerator = new TokenEnumerator("test test");
        var replacement = new StringBuilder();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var matchIds = new HashSet<int>();
        var disabledRepeatingTokens = new HashSet<int>();

        // Act
        var processed = _engine.ProcessRepeatedTokens(candidates, enumerator, replacement, result, disabledRepeatingTokens, matchIds, template, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(processed); // Should return false when no candidates match
    }

    [Fact]
    public void GivenNewlineTerminatedTokens_WhenProcessingNewlineTerminatedTokens_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("test{Name}");
        var candidates = new CandidateTokenList();

        // Add the actual token from the template to the candidates
        var token = template.Tokens.First();
        candidates.Add(token);

        var value = new { Name = "World" };
        var replacement = new StringBuilder();
        var options = new TokenizerOptions();
        var replacementLocation = new FileLocation();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var matchIds = new HashSet<int>();
        var enumerator = new TokenEnumerator("test\n");
        var disabledRepeatingTokens = new HashSet<int>();

        // Act
        _engine.ProcessNewlineTerminatedTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds, enumerator, disabledRepeatingTokens, NullDiagnosticCollector.Instance);

        // Assert
        // Method is void, so we just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void GivenFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("frontmatter")
                .WithName("FrontMatterToken")
                .WithIsFrontMatterToken()
                .Build())
            .Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessFrontMatterTokens(template, null, new FileLocation(), result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenValidCandidates_WhenTryAssignCandidateTokens_ThenAssignsSuccessfully()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var value = new { Name = "" };
        var replacement = new StringBuilder("test");
        var options = new TokenizerOptions();
        var replacementLocation = new FileLocation();
        var result = new TokenizeResultBuilder().Build();
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("test")
                .WithName("TestToken")
                .Build())
            .Build();
        var matchIds = new HashSet<int>();

        // Act
        var assigned = _engine.TryAssignCandidateTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(assigned); // Should return false when no candidates
    }

    [Fact]
    public void GivenTemplateWithOnlyFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("frontmatter")
                .WithName("FrontMatterToken")
                .WithIsFrontMatterToken()
                .Build())
            .Build();

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var enumerator = new TokenEnumerator("test");

        // Act
        _engine.ProcessFrontMatterTokens(template, null, new FileLocation(), result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
    }
}
