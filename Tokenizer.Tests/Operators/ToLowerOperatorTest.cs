using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToLowerOperatorTest
    {
        private ToLower @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToLower();
        }

        [Test]
        public void TestToLower()
        {
            var result = @operator.Perform("TEST");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void TestToLowerWhenEmpty()
        {
            var result = @operator.Perform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestToLowerWhenNull()
        {
            var result = @operator.Perform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
