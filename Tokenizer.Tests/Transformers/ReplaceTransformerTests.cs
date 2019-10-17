using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ReplaceTransformerTests
    {
        private ReplaceTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new ReplaceTransformer();
        }

        [Test]
        public void TestSubstringAfter()
        {
            var result = transformer.CanTransform("one two three", new [] { "two", "four" }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("one four three", transformed);
        }

        [Test]
        public void TestSubstringAfterWhenMissingArgument()
        {
            Assert.Throws<TokenizerException>(() => transformer.CanTransform("one two three", null, out var t));
        }

        [Test]
        public void TestSubstringAfterWhenEmpty()
        {
            var result = transformer.CanTransform(string.Empty, null, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, transformed);
        }

        [Test]
        public void TestSubstringAfterWhenNull()
        {
            var result = transformer.CanTransform(null, null, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, transformed);
        }
    }
}
