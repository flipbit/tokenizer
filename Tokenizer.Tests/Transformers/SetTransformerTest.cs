using System;
using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class SetTransformerTest
    {
        private SetTransformer transformer;

        [SetUp]
        public void SetUp()
        {
            transformer = new SetTransformer();
        }

        [Test]
        public void TestSet()
        {
            var result = transformer.CanTransform("input", new [] { "output" }, out var transformed);

            Assert.IsTrue(result);
            Assert.AreEqual("output", transformed);
        }

        [Test]
        public void TestSetWhenEmpty()
        {
            Assert.Throws<ArgumentException>(() => transformer.CanTransform(string.Empty, null, out var t));;
        }

        [Test]
        public void TestSetWhenTooManyArguments()
        {
            Assert.Throws<ArgumentException>(() => transformer.CanTransform("input", new [] { "1", "2" }, out var t));
        }

        [Test]
        public void TestInTemplate()
        {
            var pattern = @"Name: { Name : Set('Alice') }";
            var input = "Name: Bob";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual("Alice", result.Values["Name"]);
        }

        [Test]
        public void TestInTemplateWithShortHand()
        {
            var pattern = @"Name: { Name = 'Alice' : ToUpper }";
            var input = "Name: Bob";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual("ALICE", result.Values["Name"]);
        }
    }
}
