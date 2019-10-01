using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Transformers
{
    [TestFixture]
    public class SplitTransformerTest
    {
        private SplitTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new SplitTransformer();
        }

        [Test]
        public void TestSplitInput()
        {
            var result = transformer.CanTransform("1,2,3,4", new [] { "," }, out var transformed);

            Assert.IsTrue(result);

            var list = transformed as string[];

            Assert.AreEqual(4, list.Length);
            Assert.AreEqual("1", list[0]);
            Assert.AreEqual("2", list[1]);
            Assert.AreEqual("3", list[2]);
            Assert.AreEqual("4", list[3]);
        }

        [Test]
        public void TestSplitInputWhenNoSeparator()
        {
            var result = transformer.CanTransform("1-2-3-4", new [] { "," }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("1-2-3-4", transformed);
        }

        [Test]
        public void TestSplitWhenMissingArgument()
        {
            Assert.Throws<TokenizerException>(() => transformer.CanTransform("1,2,3,4", null, out var t));
        }

        [Test]
        public void TestSplitWhenEmptyInput()
        {
            var result = transformer.CanTransform(string.Empty, null, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, transformed);
        }

        [Test]
        public void TestSplitWhenNullInput()
        {
            var result = transformer.CanTransform(null, null, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, transformed);
        }
    }
}
