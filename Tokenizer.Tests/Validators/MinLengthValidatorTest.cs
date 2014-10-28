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
            var function = new Function();
            function.Parameters.Add("3");

            var result = validator.IsValid(function, null, "hello");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidMaxmiumLengthWhenInvalid()
        {
            var function = new Function();
            function.Parameters.Add("255");

            var result = validator.IsValid(function, null, "hello world");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidMaxmiumLengthWhenNoParameters()
        {
            var function = new Function();

            Assert.Throws<ValidationException>(() => validator.IsValid(function, null, "hello world"));
        }

        [Test]
        public void TestValidMaxmiumLengthWhenParametersNotAnInteger()
        {
            var function = new Function();
            function.Parameters.Add("hello");

            Assert.Throws<ValidationException>(() => validator.IsValid(function, null, "hello world"));
        }
    }
}
