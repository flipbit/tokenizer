using System;
using System.Linq;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class FunctionParserTest
    {
        private FunctionParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new FunctionParser();
        }

        [Test]
        public void TestParseFunction()
        {
            var result = parser.Parse("IsDate(dd-MMM-yyyy)").First();

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("dd-MMM-yyyy", result.Parameters[0]);
        }

        [Test]
        public void TestParseFunctionWithMultipleParamters()
        {
            var result = parser.Parse("IsDate(dd-MMM-yyyy,123)").First();

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(2, result.Parameters.Count);
            Assert.AreEqual("dd-MMM-yyyy", result.Parameters[0]);
            Assert.AreEqual("123", result.Parameters[1]);
        }

        [Test]
        public void TestParseFunctionWithNoParamters()
        {
            var result = parser.Parse("IsDate()").First();

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(0, result.Parameters.Count);;
        }

        [Test]
        public void TestParseInvalidFunction()
        {
            Assert.Throws<InvalidOperationException>(() => parser.Parse("Not a function"));
        }

        [Test]
        public void TestParseMulitpleFunctions()
        {
            var results = parser.Parse("IsDate() && MaxLength(100)");

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("IsDate", results[0].Name);
            Assert.AreEqual("MaxLength", results[1].Name);
            Assert.AreEqual("100", results[1].Parameters[0]); ;
        }
    }
}
