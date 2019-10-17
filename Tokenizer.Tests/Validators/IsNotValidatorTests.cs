using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsNotValidatorTests
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

        [Test]
        public void TestForDocumentation()
        {
            var template = "Address: { Address : IsNot('N/A'), EOL }";
            var input = "Address: N/A\nAddress: 10 Acacia Avenue";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("10 Acacia Avenue", result.First("Address"));
        }
    }
}
