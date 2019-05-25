using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsUrlValidatorTest
    {
        private IsUrlValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsUrlValidator();
        }

        [Test]
        public void TestValidateValueWhenHttp()
        {
            var result = validator.IsValid("http://github.com");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenHttps()
        {
            var result = validator.IsValid("https://github.com");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenInvalidUrl()
        {
            var result = validator.IsValid("hello world");

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
