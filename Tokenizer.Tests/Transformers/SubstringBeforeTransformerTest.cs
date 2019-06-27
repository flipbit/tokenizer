using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Transformers
{
    [TestFixture]
    public class SubstringBeforeTransformerTest
    {
        private SubstringBeforeTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new SubstringBeforeTransformer();
        }

        [Test]
        public void TestSubstringAfter()
        {
            var result = transformer.CanTransform("one two three", new [] { "two" }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("one ", transformed);
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
