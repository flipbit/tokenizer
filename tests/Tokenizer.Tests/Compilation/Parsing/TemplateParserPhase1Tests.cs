using Tokens.Compilation.Nodes;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Parsing;

public class TemplateParserPhase1Tests
{
    [Fact]
    public void GivenNoFrontMatter_WhenParsing_ThenDocumentHasNoFrontMatter()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "Hello {name}";

        // Act
        var doc = parser.Parse(input);

        // Assert
        Assert.Null(doc.FrontMatter);
        Assert.NotNull(doc.Content);
    }

    [Fact]
    public void GivenSamples_WhenParsing_ThenParsesAndProducesReasonableStructure()
    {
        // Arrange
        var parser = new TemplateParser();
        var sampleDir = System.IO.Path.Join(System.AppContext.BaseDirectory, "tests", "Tokenizer.Tests", "Samples", "Patterns");
        if (!System.IO.Directory.Exists(sampleDir)) return; // skip if not available

        // CodeQL cs/linq/missed-select: loop body has side effects (file I/O + assertions), not a pure mapping
        foreach (var file in System.IO.Directory.EnumerateFiles(sampleDir, "*.txt"))
        {
            var text = System.IO.File.ReadAllText(file);
            // Act
            var doc = parser.Parse(text);

            // Assert: should produce a content list and not throw; token names (if any) should be non-empty strings
            Assert.NotNull(doc.Content);
            // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
            foreach (var node in doc.Content)
            {
                if (node is TokenNode tn)
                {
                    Assert.False(string.IsNullOrWhiteSpace(tn.Name.Text));
                }
            }
        }
    }

    [Fact]
    public void GivenFrontMatter_WhenParsing_ThenFrontMatterPresent()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "---\nname: Template\n---\nBody";

        // Act
        var doc = parser.Parse(input);

        // Assert
        Assert.NotNull(doc.FrontMatter);
        Assert.NotNull(doc.Content);
    }

    [Fact]
    public void GivenPreambleOnly_WhenParsing_ThenProducesSingleTextNode()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "Hello world";

        // Act
        var doc = parser.Parse(input);

        // Assert
        Assert.Null(doc.FrontMatter);
        Assert.Single(doc.Content);
        Assert.IsType<TextNode>(doc.Content[0]);
    }

    [Fact]
    public void GivenSingleTokenName_WhenParsing_ThenProducesTokenNode()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "Hello {name}";

        // Act
        var doc = parser.Parse(input);

        // Assert
        Assert.Contains(doc.Content, n => n is TokenNode);
        var token = Assert.IsType<TokenNode>(doc.Content[1]);
        Assert.Equal("name", token.Name.Text);
        Assert.False(token.Modifiers.IsOptional);
        Assert.Null(token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenModifiers_WhenParsing_ThenModifierSetIsPopulated()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "{name?*!$}";

        // Act
        var doc = parser.Parse(input);

        // Assert
        var token = Assert.IsType<TokenNode>(doc.Content[0]);
        Assert.True(token.Modifiers.IsOptional);
        Assert.True(token.Modifiers.IsRepeating);
        Assert.True(token.Modifiers.IsRequired);
        Assert.True(token.Modifiers.IsTerminate);
    }

    [Fact]
    public void GivenQuotedAndUnquotedValues_WhenParsing_ThenValueNodeReflectsQuotedFlag()
    {
        var parser = new TemplateParser();

        var doc1 = parser.Parse("{id=123}");
        var t1 = Assert.IsType<TokenNode>(doc1.Content[0]);
        Assert.Equal("123", t1.Value!.Text);
        Assert.False(t1.Value.IsQuoted);

        var doc2 = parser.Parse("{user=\"Jane Doe\"}");
        var t2 = Assert.IsType<TokenNode>(doc2.Content[0]);
        Assert.Equal("Jane Doe", t2.Value!.Text);
        Assert.True(t2.Value.IsQuoted);
    }

    [Fact]
    public void GivenDecoratorsWithArgs_WhenParsing_ThenDecoratorNodesAreCreated()
    {
        var parser = new TemplateParser();
        var doc = parser.Parse("{name:trim:regex(\"[A-Z]+\", 3)}");
        var token = Assert.IsType<TokenNode>(doc.Content[0]);
        Assert.Equal(2, token.Decorators.Count);
        Assert.Equal("trim", token.Decorators[0].Name.Text);
        Assert.Equal("regex", token.Decorators[1].Name.Text);
        Assert.Equal(2, token.Decorators[1].Args.Count);
        Assert.True(token.Decorators[1].Args[0].IsQuoted);
        Assert.False(token.Decorators[1].Args[1].IsQuoted);
    }

    [Fact]
    public void GivenEscapedBracesInPreamble_WhenParsing_ThenTextContainsLiteralBraces()
    {
        var parser = new TemplateParser();
        var doc = parser.Parse("Hello {{name}} world");
        var text = Assert.IsType<TextNode>(doc.Content[0]);
        Assert.Contains("{name}", text.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenMissingCloseBrace_WhenParsing_ThenThrowsParsingException()
    {
        var parser = new TemplateParser();
        Assert.Throws<ParsingException>(() => parser.Parse("{name"));
    }

    [Fact]
    public void GivenMalformedDecoratorArgs_WhenParsing_ThenThrowsParsingException()
    {
        var parser = new TemplateParser();
        Assert.Throws<ParsingException>(() => parser.Parse("{name:regex(()}"));
        Assert.Throws<ParsingException>(() => parser.Parse("{name:regex(, )}"));
    }

    [Fact]
    public void GivenMisplacedModifiers_WhenParsing_ThenThrowsParsingException()
    {
        var parser = new TemplateParser();
        Assert.Throws<ParsingException>(() => parser.Parse("{name=1?}"));
        Assert.Throws<ParsingException>(() => parser.Parse("{name:trim?}"));
    }

    [Fact]
    public void GivenMultiLineInput_WhenParsing_ThenNodeLocationsAccurate()
    {
        // Arrange
        var parser = new TemplateParser();
        var input = "Line1\n{name}\nLine3";

        // Act
        var doc = parser.Parse(input);

        // Assert
        var token = Assert.IsType<TokenNode>(doc.Content[1]);
        var expectedStart = input.IndexOf("{name}", System.StringComparison.Ordinal);
        Assert.True(token.Location.Line >= 2);
        Assert.Equal(expectedStart, token.Start);
        Assert.True(token.Length >= 6); // at least "{name}" length
    }
}


