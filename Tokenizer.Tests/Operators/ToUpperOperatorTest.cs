using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToUpperOperatorTest
    {
        private ToUpperOperator @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToUpperOperator();
        }

        [Test]
        public void TestToUpper()
        {
            var token = new Token();

            var result = @operator.Perform(token, "test");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestToUpperWhenEmpty()
        {
            var token = new Token();

            var result = @operator.Perform(token, string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestToUpperWhenNull()
        {
            var token = new Token();

            var result = @operator.Perform(token, null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
