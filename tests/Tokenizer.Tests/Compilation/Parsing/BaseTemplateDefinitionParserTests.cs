using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation.Parsing;

public abstract class BaseTemplateDefinitionParserTests(ITestOutputHelper testOutputHelper)
{
    protected abstract ITemplateDefinitionParser Parser { get; }

    [Fact]
    public void GivenEmptyString_WhenParsing_ThenReturnsEmptyTemplate()
    {
        // Arrange & Act
        var template = Parser.Parse(string.Empty);

        // Assert
        Assert.Empty(template.Tokens);
    }

    [Fact]
    public void GivenNullString_WhenParsing_ThenThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => Parser.Parse(null!));
    }

    [Fact]
    public void GivenSingleToken_WhenParsing_ThenReturnsCorrectToken()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{TokenName}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithInvalidName_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => Parser.Parse("This is the preamble{Token Name}"));
    }

    [Fact]
    public void GivenTwoTokens_WhenParsing_ThenReturnsBothTokens()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{TokenName}Preamble 2 {TokenName2}");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens.First();

        Assert.Equal("This is the preamble", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.IsOptional);
        Assert.False(token1.TerminateOnNewLine);
        Assert.False(token1.IsRepeating);

        var token2 = template.Tokens.ElementAt(1);

        Assert.Equal("Preamble 2 ", token2.Preamble);
        Assert.Equal("TokenName2", token2.Name);
        Assert.False(token2.IsOptional);
        Assert.False(token2.TerminateOnNewLine);
        Assert.False(token2.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithNewLineTerminator_WhenParsing_ThenSetsTerminateOnNewLine()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName$}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithInvalidNewLineTerminator_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => Parser.Parse("This is the preamble{Token Name$$}"));
    }

    [Fact]
    public void GivenTokenWithOptionalTerminator_WhenParsing_ThenSetsOptional()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName?}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
        Assert.False(token.IsRequired);
    }

    [Fact]
    public void GivenTokenWithRequiredTerminator_WhenParsing_ThenSetsRequired()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName!}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRequired);
    }

    [Fact]
    public void GivenTokenWithRequiredAndOptionalCharacter_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        var e = Assert.Throws<ParsingException>(() =>
            Parser.Parse("This is the preamble{TokenName!?}"));
        testOutputHelper.WriteLine(e.Message);
    }

    [Fact]
    public void GivenTokenWithOptionalAndRequiredCharacter_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        var e = Assert.Throws<ParsingException>(() =>
            Parser.Parse("This is the preamble{TokenName?!}"));
        testOutputHelper.WriteLine(e.Message);
    }

    [Fact]
    public void GivenTokenWithOptionalAndNewLineTerminator_WhenParsing_ThenSetsBothFlags()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName$?}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithDecorator_WhenParsing_ThenAddsDecorator()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName:ToDateTime}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
        Assert.Single(token.Decorators);

        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);
    }

    [Fact]
    public void GivenTokenWithMultipleDecorators_WhenParsing_ThenAddsAllDecorators()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName:Trim,IsNotNullOrEmpty}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
        Assert.Equal(2, token.Decorators.Count);

        var decorator1 = token.Decorators.First();

        Assert.Equal("Trim", decorator1.Name);

        var decorator2 = token.Decorators.ElementAt(1);

        Assert.Equal("IsNotNullOrEmpty", decorator2.Name);
    }

    [Fact]
    public void GivenTokenWithDecoratorWithArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName:ToDateTime(yyyy-MM-dd)}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithSingleQuotedArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble{TokenName: ToDateTime ( 'yyyy-MM-dd' )}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithDoubleQuotedArgument_WhenParsing_ThenAddsDecoratorWithArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("""Preamble{TokenName: ToDateTime ( "yyyy-MM-dd" )}""");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void GivenTokenWithDecoratorWithThreeArguments_WhenParsing_ThenAddsDecoratorWithAllArguments()
    {
        // Arrange & Act
        var template = Parser.Parse(@"Preamble{TokenName:Decorator(One, Two Arg ,Three )}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("Decorator", decorator.Name);

        Assert.Equal(3, decorator.Args.Count);
        Assert.Equal("One", decorator.Args[0]);
        Assert.Equal("Two Arg", decorator.Args[1]);
        Assert.Equal("Three", decorator.Args[2]);
    }

    [Fact]
    public void GivenTokenWithTrailingText_WhenParsing_ThenCreatesSecondToken()
    {
        // Arrange & Act
        var template = Parser.Parse(@"Preamble{TokenName} Postamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal(" Postamble", second.Preamble);
    }

    [Fact]
    public void GivenTokenWithWindowsLineEndings_WhenParsing_ThenConvertsToUnixLineEndings()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble\r\n{TokenName}\r\nPostamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal("\nPostamble", second.Preamble);
    }

    [Fact]
    public void GivenTokenWithUnixLineEndings_WhenParsing_ThenPreservesUnixLineEndings()
    {
        // Arrange & Act
        var template = Parser.Parse("Preamble\n{TokenName}\nPostamble with linefeed: \r\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal("\nPostamble with linefeed: \n", second.Preamble);
    }

    [Fact]
    public void GivenFrontMatter_WhenParsing_ThenSetsOptions()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nCaseSensitive: true\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(StringComparison.InvariantCulture, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithWindowsLineEndings_WhenParsing_ThenSetsOptions()
    {
        // Arrange & Act
        var template = Parser.Parse("---\r\n# Comment\r\nCaseSensitive: false\r\n---\r\nPreamble\r\n{TokenName}\r\n");

        // Assert
        Assert.Equal(StringComparison.InvariantCultureIgnoreCase, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithName_WhenParsing_ThenSetsTemplateName()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nName: My Template\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal("My Template", template.Name);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenFrontMatterWithRequiredHint_WhenParsing_ThenAddsRequiredHint()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nHint: My Hint   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
    }

    [Fact]
    public void GivenFrontMatterWithOptionalHint_WhenParsing_ThenAddsOptionalHint()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nHint?: My Hint   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.True(template.Hints[0].Optional);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleHints_WhenParsing_ThenAddsAllHints()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nHint: My Hint   \nHint: Second Hint\n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
        Assert.Equal("Second Hint", template.Hints[1].Text);
        Assert.False(template.Hints[1].Optional);
    }

    [Fact]
    public void GivenTokenWithEscapedBrackets_WhenParsing_ThenUnescapesBrackets()
    {
        // Arrange & Act
        var template = Parser.Parse("This {{is}} the preamble{TokenName}");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This {is} the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.IsOptional);
        Assert.False(token.TerminateOnNewLine);
        Assert.False(token.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithUnescapedClosingBracket_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        try
        {
            Parser.Parse("This {{is} the preamble{TokenName}");

            Assert.Fail("Should of thrown.");
        }
        catch (ParsingException e)
        {
            Assert.Equal(1, e.Line);
            Assert.Equal(10, e.Column);
        }
    }

    [Fact]
    public void GivenTokenWithWhitespace_WhenParsing_ThenAllowsWhitespace()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName $ ! * : IsDomain , IsUrl }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.True(token.TerminateOnNewLine);
        Assert.True(token.IsRepeating);
        Assert.True(token.IsRequired);
    }

    [Fact]
    public void GivenRepeatingTokenWithNewLine_WhenParsing_ThenExpandsNewLine()
    {
        // Arrange & Act
        var template = Parser.Parse("""
                                    Repeating Token:
                                        { TokenName * }
                                    """);

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:\n    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.IsRepeating);

        var token2 = template.Tokens[1];

        Assert.Equal("\n    ", token2.Preamble);
        Assert.Equal("TokenName", token2.Name);
        Assert.True(token2.IsRepeating);
    }

    [Fact]
    public void GivenRepeatingTokenWithoutNewLine_WhenParsing_ThenDoesNotExpandNewLine()
    {
        // Arrange & Act
        var template = Parser.Parse(@"Repeating Token:    { TokenName * }");

        // Assert
        Assert.Single(template.Tokens);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.True(token1.IsRepeating);
    }

    [Fact]
    public void GivenTokenWithRequiredLonghand_WhenParsing_ThenSetsRequired()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName : Required }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRequired);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithOptionalLonghand_WhenParsing_ThenSetsOptional()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName : Optional }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsOptional);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithRepeatingLonghand_WhenParsing_ThenSetsRepeating()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName : Repeating }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.IsRepeating);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithNewLineLonghand_WhenParsing_ThenSetsTerminateOnNewLine()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName : EOL }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.TerminateOnNewLine);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenFrontMatterWithTag_WhenParsing_ThenAddsTag()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nTag: My Tag   \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Single(template.Tags);
        Assert.Equal("My Tag", template.Tags[0]);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleTags_WhenParsing_ThenAddsAllTags()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nTag: Tag One   \nTag: Tag Two  \n---\nPreamble\n{TokenName}\n");

        // Assert
        Assert.Equal(2, template.Tags.Count);
        Assert.Equal("Tag One", template.Tags[0]);
        Assert.Equal("Tag Two", template.Tags[1]);
    }

    [Fact]
    public void GivenTokenWithSetValue_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName = Foo }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueAndDecorator_WhenParsing_ThenSetsValueAndDecorator()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName = Foo : Bar }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }

    [Fact]
    public void GivenTokenWithSetValueContainingSpaces_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => Parser.Parse("This is the preamble{ TokenName = Foo Bar }"));
    }

    [Fact]
    public void GivenTokenWithSetValueContainingInvalidCharacters_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => Parser.Parse("This is the preamble{ TokenName = Foo{Bar }"));
    }

    [Fact]
    public void GivenTokenWithSetValueInDoubleQuotes_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName = \" { Foo } \" }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueInSingleQuotes_WhenParsing_ThenSetsValue()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' }");

        // Assert
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenWithSetValueInSingleQuotesAndDecorator_WhenParsing_ThenSetsValueAndDecorator()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' : Bar } Next preamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }

    [Fact]
    public void GivenFrontMatterWithSetToken_WhenParsing_ThenCreatesFrontMatterToken()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecorator_WhenParsing_ThenCreatesFrontMatterTokenWithDecorator()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken : MyDecorator \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1) \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithMultipleArguments_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndAllArguments()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1, Arg2) \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Equal(2, template.Tokens[0].Decorators[0].Args.Count);
        Assert.Equal("Arg1", template.Tokens[0].Decorators[0].Args[0]);
        Assert.Equal("Arg2", template.Tokens[0].Decorators[0].Args[1]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithDoubleQuotedArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(\"Arg1, Arg2\") \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndDecoratorWithSingleQuotedArgument_WhenParsing_ThenCreatesFrontMatterTokenWithDecoratorAndArgument()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken : MyDecorator('Arg1, Arg2') \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignment_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken = Foo \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignmentInSingleQuotes_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken = 'Foo Bar' \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithSetTokenAndAssignmentInDoubleQuotes_WhenParsing_ThenCreatesFrontMatterTokenWithValue()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n---\nPreamble\n");

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleSetTokens_WhenParsing_ThenCreatesAllFrontMatterTokens()
    {
        // Arrange & Act
        var template = Parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n  Set  : this = that : ToUpper \n---\nPreamble\n");

        // Assert
        Assert.Equal(3, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);

        Assert.Equal("this", template.Tokens[1].Name);
        Assert.True(template.Tokens[1].IsFrontMatterToken);
        Assert.Equal("that", template.Tokens[1].Value);
        Assert.Single(template.Tokens[1].Decorators);
        Assert.Equal("ToUpper", template.Tokens[1].Decorators[0].Name);
    }

    [Fact]
    public void GivenFrontMatterWithMultipleComments_WhenParsing_ThenParsesCorrectly()
    {
        // Arrange
        var content = """
                      ---
                      #
                      # .capetown Parsing Template
                      #

                      # Use this template for queries to capetown-whois.registry.net.za:
                      tag: capetown-whois.registry.net.za
                      tag: capetown

                      # Set query response type:
                      set: Response = NotFound
                      ---

                      """;

        // Act
        var template = Parser.Parse(content);

        // Assert
        Assert.Equal(2, template.Tags.Count);
    }

    [Fact]
    public void GivenNullToken_WhenParsing_ThenSetsIsNull()
    {
        // Arrange & Act
        var template = Parser.Parse("This is the preamble{ Null } Next preamble");

        // Assert
        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("Null", token.Name);
        Assert.True(token.IsNull);
    }

    [Fact]
    public void GivenNotDecorator_WhenParsing_ThenSetsIsNotDecorator()
    {
        // Arrange & Act
        var template = Parser.Parse("{ MyToken : !MyDecorator }");

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.True(template.Tokens[0].Decorators[0].IsNotDecorator);
    }

    [Fact]
    public void GivenInvalidNotDecorator_WhenParsing_ThenThrowsParsingException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ParsingException>(() => Parser.Parse("{ MyToken : Invalid!MyDecorator }"));
    }

    [Fact]
    public void GivenTemplateWithMultipleTokens_WhenParsing_ThenSetsCorrectLocations()
    {
        // Arrange
        var content = """
                      { First : Decorator('One'), Two , Three (" Four ") }
                      {Second} {Third}

                      {Fourth}
                      {Fifth}


                      {Sixth}
                      """;

        // Act
        var template = Parser.Parse(content);

        // Assert
        Assert.Equal(6, template.Tokens.Count);

        Assert.Equal("""{ First : Decorator('One'), Two, Three(' Four ') }""", template.Tokens[0].ToString());
        Assert.Equal(1, template.Tokens[0].Location.Column);
        Assert.Equal(1, template.Tokens[0].Location.Line);
        Assert.Equal(1, template.Tokens[0].Location.Paragraph);

        Assert.Equal(@"{ Second }", template.Tokens[1].ToString());
        Assert.Equal(1, template.Tokens[1].Location.Column);
        Assert.Equal(2, template.Tokens[1].Location.Line);
        Assert.Equal(1, template.Tokens[1].Location.Paragraph);

        Assert.Equal(@"{ Third }", template.Tokens[2].ToString());
        Assert.Equal(10, template.Tokens[2].Location.Column);
        Assert.Equal(2, template.Tokens[2].Location.Line);
        Assert.Equal(1, template.Tokens[2].Location.Paragraph);

        Assert.Equal(@"{ Fourth }", template.Tokens[3].ToString());
        Assert.Equal(1, template.Tokens[3].Location.Column);
        Assert.Equal(4, template.Tokens[3].Location.Line);
        Assert.Equal(2, template.Tokens[3].Location.Paragraph);

        Assert.Equal(@"{ Fifth }", template.Tokens[4].ToString());
        Assert.Equal(1, template.Tokens[4].Location.Column);
        Assert.Equal(5, template.Tokens[4].Location.Line);
        Assert.Equal(2, template.Tokens[4].Location.Paragraph);

        Assert.Equal(@"{ Sixth }", template.Tokens[5].ToString());
        Assert.Equal(1, template.Tokens[5].Location.Column);
        Assert.Equal(8, template.Tokens[5].Location.Line);
        Assert.Equal(3, template.Tokens[5].Location.Paragraph);
    }
}
