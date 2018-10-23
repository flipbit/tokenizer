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
            var result = validator.IsValid(null, null, "100");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNumericFloat()
        {
            var result = validator.IsValid(null, null, "10.0");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNotNumeric()
        {
            var result = validator.IsValid(null, null, "hello world");

            Assert.IsFalse(result);
        }
    }
}
