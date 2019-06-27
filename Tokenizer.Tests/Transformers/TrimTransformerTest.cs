using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class TrimTransformerTest
    {
        private TrimTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new TrimTransformer();
        }

        [Test]
        public void TestTrim()
        {
            var result = transformer.CanTransform("  TEST  ", null, out var t);

            Assert.AreEqual("TEST", t);
        }

        [Test]
        public void TestTrimWhenEmpty()
        {
            var result = transformer.CanTransform(string.Empty, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }

        [Test]
        public void TestTrimWhenNull()
        {
            var result = transformer.CanTransform(null, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }
    }
}
