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
            var result = @operator.CanTransform("TEST", null, out var t);

            Assert.IsTrue(result);
            Assert.AreEqual("test", t);
        }

        [Test]
        public void TestToLowerWhenEmpty()
        {
            var result = @operator.CanTransform(string.Empty, null, out var t);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, t);
        }

        [Test]
        public void TestToLowerWhenNull()
        {
            var result = @operator.CanTransform(null, null, out var t);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, t);
        }
    }
}
