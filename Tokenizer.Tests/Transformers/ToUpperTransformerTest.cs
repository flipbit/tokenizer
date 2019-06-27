using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToUpperTransformerTest
    {
        private ToUpperTransformer @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToUpperTransformer();
        }

        [Test]
        public void TestToUpper()
        {
            var result = @operator.CanTransform("test", null, out var t);

            Assert.AreEqual("TEST", t);
        }

        [Test]
        public void TestToUpperWhenEmpty()
        {
            var result = @operator.CanTransform(string.Empty, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }

        [Test]
        public void TestToUpperWhenNull()
        {
            var result = @operator.CanTransform(null, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }
    }
}
