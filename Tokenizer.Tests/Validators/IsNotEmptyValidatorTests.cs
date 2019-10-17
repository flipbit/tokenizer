using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsNotEmptyValidatorTests
    {
        private IsNotEmptyValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsNotEmptyValidator();
        }

        [Test]
        public void TestValidateValueWhenValid()
        {
            var result = validator.IsValid("hello world");

            Assert.IsTrue(result);
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
            var template = "Middle Name: { MiddleName : IsNotEmpty }";
            var input = "Middle Name:  Middle Name: Charles";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("Charles", result.First("MiddleName"));
        }
    }
}
