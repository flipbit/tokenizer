using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToUpperOperatorTest
    {
        private ToUpper @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToUpper();
        }

        [Test]
        public void TestToUpper()
        {
            var result = @operator.Perform("test");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestToUpperWhenEmpty()
        {
            var result = @operator.Perform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestToUpperWhenNull()
        {
            var result = @operator.Perform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
