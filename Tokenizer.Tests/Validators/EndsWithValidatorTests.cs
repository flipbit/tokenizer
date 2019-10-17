using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    [TestFixture]
    public class EndsWithValidatorTests
    {
        private EndsWithValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new EndsWithValidator();
        }

        [Test]
        public void TestValidateValueWhenTrue()
        {
            var result = validator.IsValid("hello world", "world");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenFalse()
        {
            var result = validator.IsValid("hello world", "hello");

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
            var template = "Email: { AdminEmail : EndsWith('admin.com') }";
            var input = "Email: alice@customer.com Email: bob@admin.com";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("bob@admin.com", result.First("AdminEmail"));
        }
    }
}
