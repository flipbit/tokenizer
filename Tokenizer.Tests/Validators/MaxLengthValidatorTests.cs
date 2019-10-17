using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    [TestFixture]
    public class MaxLengthValidatorTests
    {
        private MaxLengthValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new MaxLengthValidator();
        }

        [Test]
        public void TestValidMaximumLengthWhenValid()
        {
            var result = validator.IsValid("hello", "100");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidMaximumLengthWhenInvalid()
        {
            var result = validator.IsValid("hello world", "5");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidMaximumLengthWhenNoParameters()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world"));
        }

        [Test]
        public void TestValidMaximumLengthWhenParametersNotAnInteger()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world", "hello"));
        }

        [Test]
        public void TestForDocumentation()
        {
            var template = "Zip: { ZipCode : MaxLength(5) }";
            var input = "Zip: 123456  Zip: 78912";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("78912", result.First("ZipCode"));
        }
    }
}
