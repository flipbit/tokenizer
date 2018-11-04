using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsNumericValidatorTest
    {
        private IsNumeric validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsNumeric();
        }

        [Test]
        public void TestValidateValueWhenNumericInteger()
        {
            var result = validator.IsValid("100");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNumericFloat()
        {
            var result = validator.IsValid("10.0");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNotNumeric()
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
    }
}
