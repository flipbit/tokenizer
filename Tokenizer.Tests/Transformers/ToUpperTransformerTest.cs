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
            var result = @operator.Transform("test");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestToUpperWhenEmpty()
        {
            var result = @operator.Transform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestToUpperWhenNull()
        {
            var result = @operator.Transform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
