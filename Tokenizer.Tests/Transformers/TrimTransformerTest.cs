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
            var result = transformer.Transform("  TEST  ");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestTrimWhenEmpty()
        {
            var result = transformer.Transform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestTrimWhenNull()
        {
            var result = transformer.Transform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
