using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToUpperTransformerTests
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
            @operator.CanTransform("test", null, out var t);

            Assert.AreEqual("TEST", t);
        }

        [Test]
        public void TestToUpperWhenEmpty()
        {
            @operator.CanTransform(string.Empty, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }

        [Test]
        public void TestToUpperWhenNull()
        {
            @operator.CanTransform(null, null, out var t);

            Assert.AreEqual(string.Empty, t);
        }
    }
}
