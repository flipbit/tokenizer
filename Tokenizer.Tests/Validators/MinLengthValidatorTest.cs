using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    [TestFixture]
    public class MinLengthValidatorTest
    {
        private MinLengthValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new MinLengthValidator();
        }

        [Test]
        public void TestValidMaxmiumLengthWhenValid()
        {
            var result = validator.IsValid("hello", "3");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidMaxmiumLengthWhenInvalid()
        {
            var result = validator.IsValid("hello world", "255");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidMaxmiumLengthWhenNoParameters()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world"));
        }

        [Test]
        public void TestValidMaxmiumLengthWhenParametersNotAnInteger()
        {
            Assert.Throws<ValidationException>(() => validator.IsValid("hello world", "hello"));
        }
    }
}
