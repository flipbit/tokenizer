using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens
{
    [TestFixture]
    public class TokenValidatorFactoryTest
    {
        private TokenValidatorFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new TokenValidatorFactory();
        }

        [Test]
        public void TestFactoryLoadsValidators()
        {
            Assert.IsTrue(factory.Items.Count > 0);
        }

        [Test]
        public void TestPerformValidation()
        {
            var token = new Token { Operation = "IsNumeric()" };

            var result = factory.Validate(token, "test");

            Assert.AreEqual(false, result);
        }

        [Test]
        [Ignore]
        public void TestPerformValidationWhenInvalidValidatorSpecified()
        {
            var token = new Token { Operation = "IsMissingValidator()" };

            Assert.Throws<MissingOperationException>(() => factory.Validate(token, "test"));
        }

        [Test]
        public void TestPerformNoValidation()
        {
            var token = new Token { Operation = null };

            var result = factory.Validate(token, "test");

            Assert.AreEqual(true, result);
        }
    }
}
