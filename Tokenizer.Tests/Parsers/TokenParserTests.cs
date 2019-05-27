using System.Linq;
using NUnit.Framework;
using Tokens.Transformers;

namespace Tokens.Parsers
{
    [TestFixture]
    public class TokenParserTests
    {
        private TokenParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new TokenParser();
        }

        [Test]
        public void TestParseToken()
        {
            var template = parser.Parse("Preamble{Token:ToDateTime(yyyy-MM-dd)}", "name");

            Assert.AreEqual("name", template.Name);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual(1, token.Decorators.Count);

            var tokenOperator = token.Decorators.First();

            Assert.AreEqual(typeof(ToDateTimeTransformer), tokenOperator.DecoratorType);
            Assert.AreEqual(1, tokenOperator.Parameters.Count);
            Assert.AreEqual("yyyy-MM-dd", tokenOperator.Parameters[0]);
        }

        [Test]
        public void TestParseTokenWithTrailingNewLine()
        {
            var template = parser.Parse("Preamble{Token:ToDateTime(yyyy-MM-dd)}\n", "name");

            Assert.AreEqual("name", template.Name);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual(1, token.Decorators.Count);

            var tokenOperator = token.Decorators.First();

            Assert.AreEqual(typeof(ToDateTimeTransformer), tokenOperator.DecoratorType);
            Assert.AreEqual(1, tokenOperator.Parameters.Count);
            Assert.AreEqual("yyyy-MM-dd", tokenOperator.Parameters[0]);
        }

        [Test]
        public void TestParseTokenWithRequiredFlag()
        {
            var template = parser.Parse("Preamble{Token!}\n", "name");

            Assert.AreEqual("name", template.Name);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.IsTrue(token.Required);
        }

        [Test]
        public void TestParseSetName()
        {
            var template = parser.Parse("Preamble");

            Assert.AreEqual("Preamble", template.Name);
        }
 
        [Test]
        public void TestParseSetNameLimitToThreeWords()
        {
            var template = parser.Parse("One Two Three Four");

            Assert.AreEqual("One Two Three...", template.Name);
        }

        [Test]
        public void TestParseSetNameCountsNewLines()
        {
            var template = parser.Parse("One Two\r\nThree Four");


            Assert.AreEqual("One Two Three...", template.Name);
        }

        [Test]
        public void TestParseSetNameIgnoresFrontmatterWithWindowsNewlines()
        {
            var template = parser.Parse("---\r\nOutOfOrder: true\r\n---\r\nOne Two\r\nThree Four");

            Assert.AreEqual("One Two Three...", template.Name);
        }

        [Test]
        public void TestParseSetNameIgnoresFrontmatterWithUnixNewlines()
        {
            var template = parser.Parse("---\nOutOfOrder: true\n---\nOne Two\nThree Four");

            Assert.AreEqual("One Two Three...", template.Name);
        }

        [Test]
        public void TestParseSetNameWhenEmpty()
        {
            var template = parser.Parse("");

            Assert.AreEqual("(empty)", template.Name);
        }
    }
}
