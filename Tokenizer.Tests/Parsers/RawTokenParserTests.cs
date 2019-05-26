using System;
using System.Linq;
using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Parsers
{
    [TestFixture]
    public class RawTokenParserTests
    {
        private PreTokenParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new PreTokenParser();
        }

        [Test]
        public void TestParseEmptyString()
        {
            var template = parser.Parse(string.Empty);

            Assert.AreEqual(0, template.Tokens.Count);
        }

        [Test]
        public void TestParseNullString()
        {
            var template = parser.Parse(null);

            Assert.AreEqual(0, template.Tokens.Count);
        }

        [Test]
        public void TestParseSingleToken()
        {
            var template = parser.Parse("This is the preamble{TokenName}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("This is the preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithInvalidName()
        {
            Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{Token Name}"));
        }

        [Test]
        public void TestParseTwoTokens()
        {
            var template = parser.Parse("This is the preamble{TokenName}Preamble 2 {TokenName2}");

            Assert.AreEqual(2, template.Tokens.Count);

            var token1 = template.Tokens.First();

            Assert.AreEqual("This is the preamble", token1.Preamble);
            Assert.AreEqual("TokenName", token1.Name);
            Assert.IsFalse(token1.Optional);
            Assert.IsFalse(token1.TerminateOnNewline);
            Assert.IsFalse(token1.Repeating);

            var token2 = template.Tokens.ElementAt(1);

            Assert.AreEqual("Preamble 2 ", token2.Preamble);
            Assert.AreEqual("TokenName2", token2.Name);
            Assert.IsFalse(token2.Optional);
            Assert.IsFalse(token2.TerminateOnNewline);
            Assert.IsFalse(token2.Repeating);
        }

        [Test]
        public void TestParseTokenWithNewLineTerminator()
        {
            var template = parser.Parse("Preamble{TokenName$}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsTrue(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithNewLineTerminatorAndInvalidCharacter()
        {
            Assert.Throws<ParsingException>(() => parser.Parse("This is the preamble{Token Name$$}"));
        }

        [Test]
        public void TestParseTokenWithOptionalTerminator()
        {
            var template = parser.Parse("Preamble{TokenName?}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
            Assert.IsFalse(token.Required);
        }

        [Test]
        public void TestParseTokenWithRequiredTerminator()
        {
            var template = parser.Parse("Preamble{TokenName!}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Required);
        }

        [Test]
        public void TestParseTokenWithRequiredAndOptionalCharacter()
        {
            try
            {
                parser.Parse("This is the preamble{TokenName!?}");

                Assert.Fail("No exception thrown.");
            }
            catch (ParsingException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
            }
        }

        [Test]
        public void TestParseTokenWithOptionalAndRequiredCharacter()
        {
            try
            {
                parser.Parse("This is the preamble{TokenName?!}");

                Assert.Fail("No exception thrown.");
            }
            catch (ParsingException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Assert.Fail($"Incorrect Exception Thrown: {e.GetType().Name}");
            }
        }

        [Test]
        public void TestParseTokenWithOptionalAndNewLineTerminator()
        {
            var template = parser.Parse("Preamble{TokenName$?}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Optional);
            Assert.IsTrue(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithDecorator()
        {
            var template = parser.Parse("Preamble{TokenName:ToDateTime}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
            Assert.AreEqual(1, token.Decorators.Count);

            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);
        }

        [Test]
        public void TestParseTokenWithMultipleDecorators()
        {
            var template = parser.Parse("Preamble{TokenName:Trim,IsNotNullOrEmpty}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
            Assert.AreEqual(2, token.Decorators.Count);

            var decorator1 = token.Decorators.First();

            Assert.AreEqual("Trim", decorator1.Name);

            var decorator2 = token.Decorators.ElementAt(1);

            Assert.AreEqual("IsNotNullOrEmpty", decorator2.Name);
        }

        [Test]
        public void TestParseTokenWithDecoratorWithArgument()
        {
            var template = parser.Parse("Preamble{TokenName:ToDateTime(yyyy-MM-dd)}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithArgumentInSingleQuotes()
        {
            var template = parser.Parse("Preamble{TokenName: ToDateTime ( 'yyyy-MM-dd' )}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithArgumentInDoubleQuotes()
        {
            var template = parser.Parse(@"Preamble{TokenName: ToDateTime ( ""yyyy-MM-dd"" )}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithThreeArguments()
        {
            var template = parser.Parse(@"Preamble{TokenName:Decorator(One, Two Arg ,Three )}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("Decorator", decorator.Name);

            Assert.AreEqual(3, decorator.Args.Count);
            Assert.AreEqual("One", decorator.Args[0]);
            Assert.AreEqual("Two Arg", decorator.Args[1]);
            Assert.AreEqual("Three", decorator.Args[2]);
        }

        [Test]
        public void TestParseTokenWithTrailingText()
        {
            var template = parser.Parse(@"Preamble{TokenName} Postamble");

            Assert.AreEqual(2, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.AreEqual("TokenName", token.Name);

            var second = template.Tokens[1];
            Assert.AreEqual(string.Empty, second.Name);
            Assert.AreEqual(" Postamble", second.Preamble);
        }

        [Test]
        public void TestParseTokenConvertsWindowsLineEndingsToUnixLineEndings()
        {
            var template = parser.Parse("Preamble\r\n{TokenName}\r\nPostamble");

            Assert.AreEqual(2, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.AreEqual("Preamble\n", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);

            var second = template.Tokens[1];
            Assert.AreEqual(string.Empty, second.Name);
            Assert.AreEqual("\nPostamble", second.Preamble);
        }

        [Test]
        public void TestParseTokenPreservesUnixLineEndings()
        {
            var template = parser.Parse("Preamble\n{TokenName}\nPostamble with linefeed: \r\n");

            Assert.AreEqual(2, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.AreEqual("Preamble\n", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);

            var second = template.Tokens[1];
            Assert.AreEqual(string.Empty, second.Name);
            Assert.AreEqual("\nPostamble with linefeed: \n", second.Preamble);
        }

        [Test]
        public void TestParseFrontMatter()
        {
            var template = parser.Parse("---\n# Comment\nCaseSensitive: true\n---\nPreamble\n{TokenName}\n");

            Assert.AreEqual(StringComparison.InvariantCulture, template.Options.TokenStringComparison);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.AreEqual("Preamble\n", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
        }

        [Test]
        public void TestParseFrontMatterWithWindowsLineEndings()
        {
            var template = parser.Parse("---\r\n# Comment\r\nCaseSensitive: false\r\n---\r\nPreamble\r\n{TokenName}\r\n");

            Assert.AreEqual(StringComparison.InvariantCultureIgnoreCase, template.Options.TokenStringComparison);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.AreEqual("Preamble\n", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
        }

        [Test]
        public void TestParseFrontMatterSetsName()
        {
            var template = parser.Parse("---\n# Comment\nName: My Template\n---\nPreamble\n{TokenName}\n");

            Assert.AreEqual("My Template", template.Name);

            var token = template.Tokens.First();
            Assert.AreEqual("Preamble\n", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
        }

        [Test]
        public void TestParseFrontMatterSetsRequiredHint()
        {
            var template = parser.Parse("---\n# Comment\nHint: My Hint   \n---\nPreamble\n{TokenName}\n");

            Assert.AreEqual(1, template.Hints.Count);
            Assert.AreEqual("My Hint", template.Hints[0].Text);
            Assert.AreEqual(false, template.Hints[0].Optional);
        }

        [Test]
        public void TestParseFrontMatterSetsOptionalHint()
        {
            var template = parser.Parse("---\n# Comment\nHint?: My Hint   \n---\nPreamble\n{TokenName}\n");

            Assert.AreEqual(1, template.Hints.Count);
            Assert.AreEqual("My Hint", template.Hints[0].Text);
            Assert.AreEqual(true, template.Hints[0].Optional);
        }
        [Test]
        public void TestParseFrontMatterSetsMultipleHints()
        {
            var template = parser.Parse("---\n# Comment\nHint: My Hint   \nHint: Second Hint\n---\nPreamble\n{TokenName}\n");

            Assert.AreEqual(2, template.Hints.Count);
            Assert.AreEqual("My Hint", template.Hints[0].Text);
            Assert.AreEqual(false, template.Hints[0].Optional);
            Assert.AreEqual("Second Hint", template.Hints[1].Text);
            Assert.AreEqual(false, template.Hints[1].Optional);
        }

        [Test]
        public void TestParseTokenEscapeBrackets()
        {
            var template = parser.Parse("This {{is}} the preamble{TokenName}");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("This {is} the preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenEscapeBracketsWhenClosingBracketNotEscaped()
        {
            try
            {
                parser.Parse("This {{is} the preamble{TokenName}");

                Assert.Fail("Should of thrown.");
            }
            catch (ParsingException e)
            {
                Assert.AreEqual(1, e.Line);
                Assert.AreEqual(10, e.Column);
            }
        }
        
        [Test]
        public void TestParseTokenAllowWhiteSpace()
        {
            var template = parser.Parse("This is the preamble{ TokenName $ ! * : IsDomain , IsUrl }");

            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual("This is the preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Optional);
            Assert.IsTrue(token.TerminateOnNewline);
            Assert.IsTrue(token.Repeating);
            Assert.IsTrue(token.Required);
        }

        [Test]
        public void TestParseMultipleTokenListExpandsNewLine()
        {
            var template = parser.Parse(@"Repeating Token:
    { TokenName * }");

            Assert.AreEqual(2, template.Tokens.Count);

            var token1 = template.Tokens[0];

            Assert.AreEqual("Repeating Token:\n    ", token1.Preamble);
            Assert.AreEqual("TokenName", token1.Name);
            Assert.IsFalse(token1.Repeating);

            var token2 = template.Tokens[1];

            Assert.AreEqual("\n    ", token2.Preamble);
            Assert.AreEqual("TokenName", token2.Name);
            Assert.IsTrue(token2.Repeating);
        }

        [Test]
        public void TestParseMultipleTokenListDoesNotExpandsNewLine()
        {
            var template = parser.Parse(@"Repeating Token:    { TokenName * }");

            Assert.AreEqual(1, template.Tokens.Count);

            var token1 = template.Tokens[0];

            Assert.AreEqual("Repeating Token:    ", token1.Preamble);
            Assert.AreEqual("TokenName", token1.Name);
            Assert.IsTrue(token1.Repeating);
        }
    }
}
