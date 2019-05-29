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
            var result = transformer.Transform("input", "output");

            Assert.AreEqual("output", result);
        }

        [Test]
        public void TestSetWhenEmpty()
        {
            Assert.Throws<ArgumentException>(() => transformer.Transform(string.Empty));
        }

        [Test]
        public void TestSetWhenTooManyArguments()
        {
            Assert.Throws<ArgumentException>(() => transformer.Transform("input", "1", "2"));
        }

        [Test]
        public void TestInTemplate()
        {
            var pattern = @"Name: { Name : Set('Alice') }";
            var input = "Name: Bob";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual("Alice", result.Values["Name"]);
        }
    }
}
