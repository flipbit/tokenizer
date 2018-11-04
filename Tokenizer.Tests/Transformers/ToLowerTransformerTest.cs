using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToLowerTransformerTest
    {
        private ToLowerTransformer @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToLowerTransformer();
        }

        [Test]
        public void TestToLower()
        {
            var result = @operator.Transform("TEST");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void TestToLowerWhenEmpty()
        {
            var result = @operator.Transform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestToLowerWhenNull()
        {
            var result = @operator.Transform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
