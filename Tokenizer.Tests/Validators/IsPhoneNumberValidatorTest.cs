using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsPhoneNumberValidatorTest
    {
        private IsPhoneNumberValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsPhoneNumberValidator();
        }

        [Test]
        public void TestValidateValueWhenValidUk()
        {
            var result = validator.IsValid("01603 123123");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenValidUkWithCountryCode()
        {
            var result = validator.IsValid("+44 (0) 1603 123123");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenValidWithDots()
        {
            var result = validator.IsValid("+44.1603.123123");

            Assert.IsTrue(result);
        }
            
        [Test]
        public void TestValidateValueWhenValidWithDashes()
        {
            var result = validator.IsValid("+44-1603-123123");

            Assert.IsTrue(result);
        }
        [Test]
        public void TestValidateValueWhenValidUkWithNoAreaCode()
        {
            var result = validator.IsValid("123123");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenInvalid()
        {
            var result = validator.IsValid("hello world");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidateValueWhenTooShort()
        {
            var result = validator.IsValid("12345");

            Assert.IsFalse(result);
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
    }
}
