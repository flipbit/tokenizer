using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsDateTimeValidatorTests
    {
        private IsDateTimeValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsDateTimeValidator();
        }

        [Test]
        public void TestValidateValueWhenValid()
        {
            var result = validator.IsValid("1 May 2019");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNewIsoDate()
        {
            var result = validator.IsValid("2019-05-01");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenHasTime()
        {
            var result = validator.IsValid("2019-05-01 14:00:00");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenInvalid()
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

        [Test]
        public void TestForDocumentation()
        {
            var template = "Date: { Date : IsDateTime('yyyy-MM-dd') }";
            var input = "Date: 3rd Oct 2019 Date: 2019-10-04";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("2019-10-04", result.First("Date"));
        }
    }
}
