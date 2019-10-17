using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    [TestFixture]
    public class StartsWithValidatorTests
    {
        private StartsWithValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new StartsWithValidator();
        }

        [Test]
        public void TestValidateValueWhenTrue()
        {
            var result = validator.IsValid("hello world", "hello");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenFalse()
        {
            var result = validator.IsValid("hello world", "world");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidateValueWhenMissingArgument()
        {
            Assert.Throws<TokenizerException>(() => validator.IsValid("hello world"));
        }

        [Test]
        public void TestValidateValueWhenNull()
        {
            var result = validator.IsValid(null);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidateValueWhenEmpty()
        {
            var result = validator.IsValid(string.Empty);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestForDocumentation()
        {
            var template = "Ip: { InternalIpAddress : StartsWith('192') }";
            var input = "Ip: 80.34.123.45  Ip: 192.168.1.1";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("192.168.1.1", result.First("InternalIpAddress"));
        }
    }
}
