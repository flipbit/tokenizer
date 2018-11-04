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
            var result = transformer.Transform("one two three", "two");

            Assert.AreEqual("one ", result);
        }

        [Test]
        public void TestSubstringAfterWhenMissingArgument()
        {
            Assert.Throws<TokenizerException>(() => transformer.Transform("one two three"));
        }

        [Test]
        public void TestSubstringAfterWhenEmpty()
        {
            var result = transformer.Transform(string.Empty);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TestSubstringAfterhenNull()
        {
            var result = transformer.Transform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
