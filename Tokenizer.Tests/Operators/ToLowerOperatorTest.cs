using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToLowerOperatorTest
    {
        private ToLowerOperator @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToLowerOperator();
        }

        [Test]
        public void TestToUpper()
        {
            var token = new Token();

            var result = @operator.Perform(token, "TEST");

            Assert.AreEqual("test", result);
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
