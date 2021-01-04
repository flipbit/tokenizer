using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsLooseAbsoluteUrlValidatorTests
    {
        private IsLooseAbsoluteUrlValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsLooseAbsoluteUrlValidator();
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
        public void TestValidateValueWhenNoProtocol()
        {
            var result = validator.IsValid("github.com");

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

        [Test]
        public void TestForDocumentation()
        {
            var template = "Server: { ServerUrl : IsLooseAbsoluteUrl, EOL }";
            var input = "Server: Not Specified\nServer: www.server.com";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("www.server.com", result.First("ServerUrl"));
        }
    }
}
