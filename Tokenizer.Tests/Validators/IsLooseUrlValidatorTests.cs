using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsLooseUrlValidatorTests
    {
        private IsLooseUrlValidator validator;

        [SetUp]
        public void SetUp()
        {
            validator = new IsLooseUrlValidator();
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
            var template = "Server: { ServerUrl : IsUrl, EOL }";
            var input = "Server: 192.168.1.1\nServer: http://www.server.com";

            var result = new Tokenizer().Tokenize(template, input);

            Assert.AreEqual("http://www.server.com", result.First("ServerUrl"));
        }
    }
}
