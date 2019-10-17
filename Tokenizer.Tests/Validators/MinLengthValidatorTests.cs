using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    [TestFixture]
    public class MinLengthValidatorTests
    {
        private MinLengthValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new MinLengthValidator();
        }

        [Test]
        public void TestValidMinimumLengthWhenValid()
        {
            var result = validator.IsValid("hello", "3");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidMinimumLengthWhenInvalid()
        {
            var result = validator.IsValid("hello world", "255");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidMinimumLengthWhenNoParameters()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world"));
        }

        [Test]
        public void TestValidMinimumLengthWhenParametersNotAnInteger()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world", "hello"));
        }

        [Test]
        public void TestForDocumentation()
        {
            var template = "Zip: { ZipCode : MinLength(5), EOL }";
            var input = "Zip: 123\nZip: 45678";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("45678", result.First("ZipCode"));
        }
    }
}
