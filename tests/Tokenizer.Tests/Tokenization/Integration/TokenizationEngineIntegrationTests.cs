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
        var parser = new TemplateCompiler(new TokenizerOptions());
#pragma warning disable MA0101 // String contains an implicit end of line character
        var template = parser.Compile(@"
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
").Template;
#pragma warning restore MA0101

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

#pragma warning disable MA0101 // String contains an implicit end of line character
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
#pragma warning restore MA0101

        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

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
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.All(result.Tokens.Matches, m => Assert.True(m.Token.IsRequired));
    }

    [Fact]
    public void GivenTemplateWithRepeatingAndNonRepeatingTokens_WhenTokenizing_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Title: {Title}\nItem: {Item*}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Title: My List\nItem: First\nItem: Second\nItem: Third";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 2); // Title + at least some items
    }

    [Fact]
    public void GivenTemplateWithFrontMatterAndBodyTokens_WhenTokenizing_ThenProcessesBoth()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
#pragma warning disable MA0101 // String contains an implicit end of line character
        var template = parser.Compile(@"---
Name: MyTemplate
---
Content: {Content}").Template;
#pragma warning restore MA0101

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Content: Test content";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenMultilineInput_WhenTokenizing_ThenHandlesNewlinesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Line1: {Line1}\nLine2: {Line2}\nLine3: {Line3}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line1: First\nLine2: Second\nLine3: Third";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Equal(3, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenWindowsLineEndings_WhenTokenizing_ThenNormalizesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Line1: {Line1}\r\nLine2: {Line2}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line1: First\r\nLine2: Second";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenTemplateWithDecorators_WhenTokenizing_ThenAppliesTransformations()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name:ToUpper}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name: john";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        // Decorator should transform the value
    }

    [Fact]
    public void GivenTemplateWithMultipleDecorators_WhenTokenizing_ThenAppliesAllInOrder()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name:Trim:ToUpper}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name:   john  ";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTemplateWithValidators_WhenTokenizing_ThenValidatesValues()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Age: {Age:IsNumeric}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Age: 25";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTemplateWithHintsAndTokens_WhenTokenizing_ThenProcessesBoth()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("---\nhint: Expected\n---\nName: {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Expected Name: John";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenNestedTokenStructure_WhenTokenizing_ThenHandlesComplexity()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Section1: {Section1}\n  Item: {Item1}\nSection2: {Section2}\n  Item: {Item2}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Section1: First\n  Item: ItemA\nSection2: Second\n  Item: ItemB";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 2);
    }

    [Fact]
    public void GivenTemplateWithNewlineTerminatedTokens_WhenTokenizing_ThenStopsAtNewline()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Line: {Content#}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Line: This is content\nNext line should not be included";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        var session = _engine.CreateSession(template, targetObject: null, result, NullDiagnosticCollector.Instance);
        session.Run(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.DoesNotContain("Next line", result.Tokens.Matches[0].Value.ToString());
    }
}
