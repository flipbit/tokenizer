using System;
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
            var result = parser.Parse("IsDate(dd-MMM-yyyy)");

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(1, result.Parameters.Count);
            Assert.AreEqual("dd-MMM-yyyy", result.Parameters[0]);
        }

        [Test]
        public void TestParseFunctionWithMultipleParamters()
        {
            var result = parser.Parse("IsDate(dd-MMM-yyyy,123)");

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(2, result.Parameters.Count);
            Assert.AreEqual("dd-MMM-yyyy", result.Parameters[0]);
            Assert.AreEqual("123", result.Parameters[1]);
        }

        [Test]
        public void TestParseFunctionWithNoParamters()
        {
            var result = parser.Parse("IsDate()");

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
            var result = parser.Parse("IsDate(),MaxLength(100)");

            Assert.AreEqual("IsDate", result.Name);
            Assert.AreEqual(0, result.Parameters.Count); ;
        }
    }
}
