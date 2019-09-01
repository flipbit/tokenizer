using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsNotValidatorTest
    {
        private IsNotValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsNotValidator();
        }

        [Test]
        public void TestValidateValueWhenInvalid()
        {
            var result = validator.IsValid("hello world", "hello world");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidateValueWhenValid()
        {
            var result = validator.IsValid("hello world", "hello");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNull()
        {
            var result = validator.IsValid(null, "hello");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenEmpty()
        {
            var result = validator.IsValid(string.Empty, "hello");

            Assert.IsTrue(result);
        }
    }
}
