using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Transformers
{
    [TestFixture]
    public class RemoveEndTransformerTests
    {
        private RemoveEndTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new RemoveEndTransformer();
        }

        [Test]
        public void TestRemoveEnd()
        {
            var result = transformer.CanTransform("one two three", new [] { "three" }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("one two ", transformed);
        }

        [Test]
        public void TestRemoveEndWhenNotPresent()
        {
            var result = transformer.CanTransform("one two three", new [] { "two" }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("one two three", transformed);
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

        [Test]
        public void TestForDocumentation()
        {
            var template = "Domain Name: { DomainName : RemoveEnd('.') }";
            var input = "Domain Name: domain.com.";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("domain.com", result.First("DomainName"));
        }
    }
}
