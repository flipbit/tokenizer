using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsDomainNameValidatorTest
    {
        private IsDomainNameValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsDomainNameValidator();
        }

        [Test]
        public void TestValidateValueWhenValid()
        {
            var result = validator.IsValid("github.com");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNewTld()
        {
            var result = validator.IsValid("hello.ninja");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenHasSubdomain()
        {
            var result = validator.IsValid("www.hello.ninja");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenInvalidDomain()
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
