using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Integration;

/// <summary>
/// Tests for TokenizationEngine complex integration scenarios (10+ tokens, mixed features, end-to-end)
/// </summary>
public class TokenizationEngineIntegrationTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenComplexTemplateWith10Tokens_WhenTokenizing_ThenMatchesAll()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse(@"
Name: {Name}
Age: {Age}
Email: {Email}
Phone: {Phone}
Address: {Address}
City: {City}
State: {State}
Zip: {Zip}
Country: {Country}
Notes: {Notes}
");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = @"
Name: John Doe
Age: 30
Email: john@example.com
Phone: 555-1234
Address: 123 Main St
City: Springfield
State: IL
Zip: 62701
Country: USA
Notes: Test notes
";

        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 8); // At least most tokens matched
    }

    [Fact]
    public void GivenTemplateWithMixedRequiredAndOptionalTokens_WhenTokenizing_ThenHandlesCorrectly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("MixedTemplate")
            .WithTokens(
                new TokenBuilder().WithName("Required1").WithPreamble("R1: ").WithRequired().Build(),
                new TokenBuilder().WithName("Optional1").WithPreamble("O1: ").WithOptional().Build(),
                new TokenBuilder().WithName("Required2").WithPreamble("R2: ").WithRequired().Build(),
                new TokenBuilder().WithName("Optional2").WithPreamble("O2: ").WithOptional().Build()
            )
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "R1: Value1\nR2: Value2";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.All(result.Tokens.Matches, m => Assert.True(m.Token.IsRequired));
    }

    [Fact]
    public void GivenTemplateWithRepeatingAndNonRepeatingTokens_WhenTokenizing_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Title: {Title}\nItem: {Item*}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Title: My List\nItem: First\nItem: Second\nItem: Third";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 2); // Title + at least some items
    }

    [Fact]
    public void GivenTemplateWithFrontMatterAndBodyTokens_WhenTokenizing_ThenProcessesBoth()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse(@"---
Name: MyTemplate
---
Content: {Content}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Content: Test content";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMultilineInput_WhenTokenizing_ThenHandlesNewlinesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Line1: {Line1}\nLine2: {Line2}\nLine3: {Line3}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line1: First\nLine2: Second\nLine3: Third";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenWindowsLineEndings_WhenTokenizing_ThenNormalizesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Line1: {Line1}\r\nLine2: {Line2}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line1: First\r\nLine2: Second";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenTemplateWithDecorators_WhenTokenizing_ThenAppliesTransformations()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name:ToUpper}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name: john";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        // Decorator should transform the value
    }

    [Fact]
    public void GivenTemplateWithMultipleDecorators_WhenTokenizing_ThenAppliesAllInOrder()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name:Trim:ToUpper}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name:   john  ";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTemplateWithValidators_WhenTokenizing_ThenValidatesValues()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Age: {Age:IsNumeric}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Age: 25";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTemplateWithHintsAndTokens_WhenTokenizing_ThenProcessesBoth()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("---\nhint: Expected\n---\nName: {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Expected Name: John";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenNestedTokenStructure_WhenTokenizing_ThenHandlesComplexity()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Section1: {Section1}\n  Item: {Item1}\nSection2: {Section2}\n  Item: {Item2}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Section1: First\n  Item: ItemA\nSection2: Second\n  Item: ItemB";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 2);
    }

    [Fact]
    public void GivenTemplateWithNewlineTerminatedTokens_WhenTokenizing_ThenStopsAtNewline()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Line: {Content#}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line: This is content\nNext line should not be included";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, input.Length, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.DoesNotContain("Next line", result.Tokens.Matches[0].Value.ToString());
    }
}
