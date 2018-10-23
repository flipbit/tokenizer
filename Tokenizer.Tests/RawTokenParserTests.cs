using System.Linq;
using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens
{
    [TestFixture]
    public class RawTokenParserTests
    {
        private RawTokenParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new RawTokenParser();
        }

        [Test]
        public void TestParseEmptyString()
        {
            var tokens = parser.Parse(string.Empty);

            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void TestParseNullString()
        {
            var tokens = parser.Parse(null);

            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void TestParseSingleToken()
        {
            var tokens = parser.Parse("This is the preamble{TokenName}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

            Assert.AreEqual("This is the preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithInvalidName()
        {
            Assert.Throws<TokenizerException>(() => parser.Parse("This is the preamble{Token Name}"));
        }

        [Test]
        public void TestParseTwoTokens()
        {
            var tokens = parser.Parse("This is the preamble{TokenName}Preamble 2 {TokenName2}");

            Assert.AreEqual(2, tokens.Count);

            var token1 = tokens.First();

            Assert.AreEqual("This is the preamble", token1.Preamble);
            Assert.AreEqual("TokenName", token1.Name);
            Assert.IsFalse(token1.Optional);
            Assert.IsFalse(token1.TerminateOnNewline);
            Assert.IsFalse(token1.Repeating);

            var token2 = tokens.ElementAt(1);

            Assert.AreEqual("Preamble 2 ", token2.Preamble);
            Assert.AreEqual("TokenName2", token2.Name);
            Assert.IsFalse(token2.Optional);
            Assert.IsFalse(token2.TerminateOnNewline);
            Assert.IsFalse(token2.Repeating);
        }

        [Test]
        public void TestParseTokenWithNewLineTerminator()
        {
            var tokens = parser.Parse("Preamble{TokenName$}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsTrue(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithNewLineTerminatorAndInvalidCharacter()
        {
            Assert.Throws<TokenizerException>(() => parser.Parse("This is the preamble{Token Name$$}"));
        }

        [Test]
        public void TestParseTokenWithOptionalTerminator()
        {
            var tokens = parser.Parse("Preamble{TokenName?}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithOptionalAndNewLineTerminator()
        {
            var tokens = parser.Parse("Preamble{TokenName$?}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

            Assert.AreEqual("Preamble", token.Preamble);
            Assert.AreEqual("TokenName", token.Name);
            Assert.IsTrue(token.Optional);
            Assert.IsTrue(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }

        [Test]
        public void TestParseTokenWithDecorator()
        {
            var tokens = parser.Parse("Preamble{TokenName:ToDateTime}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

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
            var tokens = parser.Parse("Preamble{TokenName:Trim,IsNotNullOrEmpty}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

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
            var tokens = parser.Parse("Preamble{TokenName:ToDateTime(yyyy-MM-dd)}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithArgumentInSingleQuotes()
        {
            var tokens = parser.Parse("Preamble{TokenName: ToDateTime ( 'yyyy-MM-dd' )}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithArgumentInDoubleQuotes()
        {
            var tokens = parser.Parse(@"Preamble{TokenName: ToDateTime ( ""yyyy-MM-dd"" )}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();
            var decorator = token.Decorators.First();

            Assert.AreEqual("ToDateTime", decorator.Name);

            Assert.AreEqual(1, decorator.Args.Count);
            Assert.AreEqual("yyyy-MM-dd", decorator.Args.First());
        }

        [Test]
        public void TestParseTokenWithDecoratorWithThreeArguments()
        {
            var tokens = parser.Parse(@"Preamble{TokenName:Decorator(One, Two Arg ,Three )}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();
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
            var tokens = parser.Parse(@"Preamble{TokenName} Postamble");

            Assert.AreEqual(2, tokens.Count);

            var token = tokens.First();
            Assert.AreEqual("TokenName", token.Name);

            var second = tokens[1];
            Assert.AreEqual(null, second.Name);
            Assert.AreEqual(" Postamble", second.Preamble);
        }
    }
}
