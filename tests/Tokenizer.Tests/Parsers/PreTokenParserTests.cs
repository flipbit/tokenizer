using System;
using System.Linq;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Parsers;

public class PreTokenParserTests(ITestOutputHelper testOutputHelper)
{
    private readonly PreTokenParser parser = new();

    [Fact]
    public void TestParseEmptyString()
    {
        var template = parser.Parse(string.Empty);

        Assert.Empty(template.Tokens);
    }

    [Fact]
    public void TestParseNullString()
    {
        var template = parser.Parse(null);

        Assert.Empty(template.Tokens);
    }

    [Fact]
    public void TestParseSingleToken()
    {
        var template = parser.Parse("This is the preamble{TokenName}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
    }

    [Fact]
    public void TestParseTokenWithInvalidName()
    {
        Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{Token Name}"));
    }

    [Fact]
    public void TestParseTwoTokens()
    {
        var template = parser.Parse("This is the preamble{TokenName}Preamble 2 {TokenName2}");

        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens.First();

        Assert.Equal("This is the preamble", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.Optional);
        Assert.False(token1.TerminateOnNewline);
        Assert.False(token1.Repeating);

        var token2 = template.Tokens.ElementAt(1);

        Assert.Equal("Preamble 2 ", token2.Preamble);
        Assert.Equal("TokenName2", token2.Name);
        Assert.False(token2.Optional);
        Assert.False(token2.TerminateOnNewline);
        Assert.False(token2.Repeating);
    }

    [Fact]
    public void TestParseTokenWithNewLineTerminator()
    {
        var template = parser.Parse("Preamble{TokenName$}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.True(token.TerminateOnNewline);
        Assert.False(token.Repeating);
    }

    [Fact]
    public void TestParseTokenWithNewLineTerminatorAndInvalidCharacter()
    {
        Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{Token Name$$}"));
    }

    [Fact]
    public void TestParseTokenWithOptionalTerminator()
    {
        var template = parser.Parse("Preamble{TokenName?}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.False(token.Required);
    }

    [Fact]
    public void TestParseTokenWithRequiredTerminator()
    {
        var template = parser.Parse("Preamble{TokenName!}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Required);
    }

    [Fact]
    public void TestParseTokenWithRequiredAndOptionalCharacter()
    {
        try
        {
            parser.Parse("This is the preamble{TokenName!?}");

            Assert.Fail("No exception thrown.");
        }
        catch (ParsingException e)
        {
            testOutputHelper.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
        }
    }

    [Fact]
    public void TestParseTokenWithOptionalAndRequiredCharacter()
    {
        try
        {
            parser.Parse("This is the preamble{TokenName?!}");

            Assert.Fail("No exception thrown.");
        }
        catch (ParsingException e)
        {
            testOutputHelper.WriteLine(e.Message);
        }
        catch (Exception e)
        {
            Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
        }
    }

    [Fact]
    public void TestParseTokenWithOptionalAndNewLineTerminator()
    {
        var template = parser.Parse("Preamble{TokenName$?}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Optional);
        Assert.True(token.TerminateOnNewline);
        Assert.False(token.Repeating);
    }

    [Fact]
    public void TestParseTokenWithDecorator()
    {
        var template = parser.Parse("Preamble{TokenName:ToDateTime}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.Single(token.Decorators);

        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);
    }

    [Fact]
    public void TestParseTokenWithMultipleDecorators()
    {
        var template = parser.Parse("Preamble{TokenName:Trim,IsNotNullOrEmpty}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("Preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.Equal(2, token.Decorators.Count);

        var decorator1 = token.Decorators.First();

        Assert.Equal("Trim", decorator1.Name);

        var decorator2 = token.Decorators.ElementAt(1);

        Assert.Equal("IsNotNullOrEmpty", decorator2.Name);
    }

    [Fact]
    public void TestParseTokenWithDecoratorWithArgument()
    {
        var template = parser.Parse("Preamble{TokenName:ToDateTime(yyyy-MM-dd)}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void TestParseTokenWithDecoratorWithArgumentInSingleQuotes()
    {
        var template = parser.Parse("Preamble{TokenName: ToDateTime ( 'yyyy-MM-dd' )}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void TestParseTokenWithDecoratorWithArgumentInDoubleQuotes()
    {
        var template = parser.Parse("""Preamble{TokenName: ToDateTime ( "yyyy-MM-dd" )}""");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        var decorator = token.Decorators.First();

        Assert.Equal("ToDateTime", decorator.Name);

        Assert.Single(decorator.Args);
        Assert.Equal("yyyy-MM-dd", decorator.Args.First());
    }

    [Fact]
    public void TestParseTokenWithDecoratorWithThreeArguments()
    {
        var template = parser.Parse(@"Preamble{TokenName:Decorator(One, Two Arg ,Three )}");

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
    public void TestParseTokenWithTrailingText()
    {
        var template = parser.Parse(@"Preamble{TokenName} Postamble");

        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal(" Postamble", second.Preamble);
    }

    [Fact]
    public void TestParseTokenConvertsWindowsLineEndingsToUnixLineEndings()
    {
        var template = parser.Parse("Preamble\r\n{TokenName}\r\nPostamble");

        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal("\nPostamble", second.Preamble);
    }

    [Fact]
    public void TestParseTokenPreservesUnixLineEndings()
    {
        var template = parser.Parse("Preamble\n{TokenName}\nPostamble with linefeed: \r\n");

        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);

        var second = template.Tokens[1];
        Assert.Equal(string.Empty, second.Name);
        Assert.Equal("\nPostamble with linefeed: \n", second.Preamble);
    }

    [Fact]
    public void TestParseFrontMatter()
    {
        var template = parser.Parse("---\n# Comment\nCaseSensitive: true\n---\nPreamble\n{TokenName}\n");

        Assert.Equal(StringComparison.InvariantCulture, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void TestParseFrontMatterWithWindowsLineEndings()
    {
        var template = parser.Parse("---\r\n# Comment\r\nCaseSensitive: false\r\n---\r\nPreamble\r\n{TokenName}\r\n");

        Assert.Equal(StringComparison.InvariantCultureIgnoreCase, template.Options.TokenStringComparison);
        Assert.Single(template.Tokens);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void TestParseFrontMatterSetsName()
    {
        var template = parser.Parse("---\n# Comment\nName: My Template\n---\nPreamble\n{TokenName}\n");

        Assert.Equal("My Template", template.Name);

        var token = template.Tokens.First();
        Assert.Equal("Preamble\n", token.Preamble);
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void TestParseFrontMatterSetsRequiredHint()
    {
        var template = parser.Parse("---\n# Comment\nHint: My Hint   \n---\nPreamble\n{TokenName}\n");

        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
    }

    [Fact]
    public void TestParseFrontMatterSetsOptionalHint()
    {
        var template = parser.Parse("---\n# Comment\nHint?: My Hint   \n---\nPreamble\n{TokenName}\n");

        Assert.Single(template.Hints);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.True(template.Hints[0].Optional);
    }

    [Fact]
    public void TestParseFrontMatterSetsMultipleHints()
    {
        var template = parser.Parse("---\n# Comment\nHint: My Hint   \nHint: Second Hint\n---\nPreamble\n{TokenName}\n");

        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("My Hint", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
        Assert.Equal("Second Hint", template.Hints[1].Text);
        Assert.False(template.Hints[1].Optional);
    }

    [Fact]
    public void TestParseTokenEscapeBrackets()
    {
        var template = parser.Parse("This {{is}} the preamble{TokenName}");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This {is} the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
    }

    [Fact]
    public void TestParseTokenEscapeBracketsWhenClosingBracketNotEscaped()
    {
        try
        {
            parser.Parse("This {{is} the preamble{TokenName}");

            Assert.Fail("Should of thrown.");
        }
        catch (ParsingException e)
        {
            Assert.Equal(1, e.Line);
            Assert.Equal(10, e.Column);
        }
    }
        
    [Fact]
    public void TestParseTokenAllowWhiteSpace()
    {
        var template = parser.Parse("This is the preamble{ TokenName $ ! * : IsDomain , IsUrl }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Optional);
        Assert.True(token.TerminateOnNewline);
        Assert.True(token.Repeating);
        Assert.True(token.Required);
    }

    [Fact]
    public void TestParseMultipleTokenListExpandsNewLine()
    {
        var template = parser.Parse("""
                                    Repeating Token:
                                        { TokenName * }
                                    """);

        Assert.Equal(2, template.Tokens.Count);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:\n    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.False(token1.Repeating);

        var token2 = template.Tokens[1];

        Assert.Equal("\n    ", token2.Preamble);
        Assert.Equal("TokenName", token2.Name);
        Assert.True(token2.Repeating);
    }

    [Fact]
    public void TestParseMultipleTokenListDoesNotExpandsNewLine()
    {
        var template = parser.Parse(@"Repeating Token:    { TokenName * }");

        Assert.Single(template.Tokens);

        var token1 = template.Tokens[0];

        Assert.Equal("Repeating Token:    ", token1.Preamble);
        Assert.Equal("TokenName", token1.Name);
        Assert.True(token1.Repeating);
    }
        
    [Fact]
    public void TestParseTokenRequiredLonghand()
    {
        var template = parser.Parse("This is the preamble{ TokenName : Required }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Required);
        Assert.Empty(token.Decorators);
    }
        
    [Fact]
    public void TestParseTokenOptionalLonghand()
    {
        var template = parser.Parse("This is the preamble{ TokenName : Optional }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Optional);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void TestParseTokenRepeatingLonghand()
    {
        var template = parser.Parse("This is the preamble{ TokenName : Repeating }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.Repeating);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void TestParseTokenNewLineLonghand()
    {
        var template = parser.Parse("This is the preamble{ TokenName : EOL }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.True(token.TerminateOnNewline);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void TestParseFrontMatterSetsTag()
    {
        var template = parser.Parse("---\n# Comment\nTag: My Tag   \n---\nPreamble\n{TokenName}\n");

        Assert.Single(template.Tags);
        Assert.Equal("My Tag", template.Tags[0]);
    }

    [Fact]
    public void TestParseFrontMatterSetsMultipleTags()
    {
        var template = parser.Parse("---\n# Comment\nTag: Tag One   \nTag: Tag Two  \n---\nPreamble\n{TokenName}\n");

        Assert.Equal(2, template.Tags.Count);
        Assert.Equal("Tag One", template.Tags[0]);
        Assert.Equal("Tag Two", template.Tags[1]);
    }

    [Fact]
    public void TestParseTokenSetValue()
    {
        var template = parser.Parse("This is the preamble{ TokenName = Foo }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Empty(token.Decorators);
    }


    [Fact]
    public void TestParseTokenSetValueWithDecorator()
    {
        var template = parser.Parse("This is the preamble{ TokenName = Foo : Bar }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal("Foo", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }

    [Fact]
    public void TestParseTokenSetValueFailsWhenContainsSpaces()
    {
        Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{ TokenName = Foo Bar }"));
    }

    [Fact]
    public void TestParseTokenSetValueFailsWhenContainsInvalidCharacters()
    {
        Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{ TokenName = Foo{Bar }"));
    }

    [Fact]
    public void TestParseTokenSetValueInDoubleQuotes()
    {
        var template = parser.Parse("This is the preamble{ TokenName = \" { Foo } \" }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void TestParseTokenSetValueInSingleQuotes()
    {
        var template = parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' }");

        Assert.Single(template.Tokens);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void TestParseTokenSetValueInSingleQuotesWithDecorator()
    {
        var template = parser.Parse("This is the preamble{ TokenName = ' { Foo } \" ' : Bar } Next preamble");

        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("TokenName", token.Name);
        Assert.Equal(" { Foo } \" ", token.Value);
        Assert.Single(token.Decorators);
        Assert.Equal("Bar", token.Decorators[0].Name);
    }

    [Fact]
    public void TestParseFrontMatterSetsToken()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndDecorator()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken : MyDecorator \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndDecoratorWithArgument()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1) \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndDecoratorWithMultipleArguments()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(Arg1, Arg2) \n---\nPreamble\n");

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
    public void TestParseFrontMatterSetsTokenAndDecoratorWithDoubleQuotedArgument()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken : MyDecorator(\"Arg1, Arg2\") \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndDecoratorWithSingleQuotedArgument()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken : MyDecorator('Arg1, Arg2') \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.Single(template.Tokens[0].Decorators[0].Args);
        Assert.Equal("Arg1, Arg2", template.Tokens[0].Decorators[0].Args[0]);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndAssignment()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken = Foo \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo", template.Tokens[0].Value);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndAssignmentInSingleQuotes()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken = 'Foo Bar' \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void TestParseFrontMatterSetsTokenAndAssignmentInDoubleQuotes()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n---\nPreamble\n");

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
        Assert.Equal("Foo Bar", template.Tokens[0].Value);
    }

    [Fact]
    public void TestParseFrontMatterSetsMultipleTokens()
    {
        var template = parser.Parse("---\n# Comment\nset: MyToken = \"Foo Bar\" \n  Set  : this = that : ToUpper \n---\nPreamble\n");

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
    public void TestParseFrontMatterWithMultipleComments()
    {
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

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tags.Count);
    }

    [Fact]
    public void TestParseNullToken()
    {
        var template = parser.Parse("This is the preamble{ Null } Next preamble");

        Assert.Equal(2, template.Tokens.Count);

        var token = template.Tokens.First();

        Assert.Equal("This is the preamble", token.Preamble);
        Assert.Equal("Null", token.Name);
        Assert.True(token.IsNull);
    }

    [Fact]
    public void TestParseNotDecorator()
    {
        var template = parser.Parse("{ MyToken : !MyDecorator }");

        Assert.Single(template.Tokens);
        Assert.Equal("MyToken", template.Tokens[0].Name);
        Assert.Single(template.Tokens[0].Decorators);
        Assert.Equal("MyDecorator", template.Tokens[0].Decorators[0].Name);
        Assert.True(template.Tokens[0].Decorators[0].IsNotDecorator);
    }

    [Fact]
    public void TestParseNotDecoratorThrowsException()
    {
        Assert.Throws<ParsingException>(() => parser.Parse("{ MyToken : Invalid!MyDecorator }"));
    }
        
    [Fact]
    public void TestParseTemplateLocations()
    {
        var content = """
                      { First : Decorator('One'), Two , Three (" Four ") }
                      {Second} {Third}

                      {Fourth}
                      {Fifth}


                      {Sixth}
                      """;

        var template = parser.Parse(content);

        Assert.Equal(6, template.Tokens.Count);

        Assert.Equal("""{ First : Decorator('One'), Two , Three (" Four ") }""", template.Tokens[0].ToString());
        Assert.Equal(1, template.Tokens[0].Location.Column);
        Assert.Equal(1, template.Tokens[0].Location.Line);
        Assert.Equal(1, template.Tokens[0].Location.Paragraph);

        Assert.Equal(@"{Second}", template.Tokens[1].ToString());
        Assert.Equal(1, template.Tokens[1].Location.Column);
        Assert.Equal(2, template.Tokens[1].Location.Line);
        Assert.Equal(1, template.Tokens[1].Location.Paragraph);

        Assert.Equal(@"{Third}", template.Tokens[2].ToString());
        Assert.Equal(10, template.Tokens[2].Location.Column);
        Assert.Equal(2, template.Tokens[2].Location.Line);
        Assert.Equal(1, template.Tokens[2].Location.Paragraph);

        Assert.Equal(@"{Fourth}", template.Tokens[3].ToString());
        Assert.Equal(1, template.Tokens[3].Location.Column);
        Assert.Equal(4, template.Tokens[3].Location.Line);
        Assert.Equal(2, template.Tokens[3].Location.Paragraph);

        Assert.Equal(@"{Fifth}", template.Tokens[4].ToString());
        Assert.Equal(1, template.Tokens[4].Location.Column);
        Assert.Equal(5, template.Tokens[4].Location.Line);
        Assert.Equal(2, template.Tokens[4].Location.Paragraph);

        Assert.Equal(@"{Sixth}", template.Tokens[5].ToString());
        Assert.Equal(1, template.Tokens[5].Location.Column);
        Assert.Equal(8, template.Tokens[5].Location.Line);
        Assert.Equal(3, template.Tokens[5].Location.Paragraph);
    }
}