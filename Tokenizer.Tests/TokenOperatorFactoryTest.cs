using NUnit.Framework;
using Tokens.Exceptions;

namespace Tokens
{
    [TestFixture]
    public class TokenOperatorFactoryTest
    {
        private TokenOperatorFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = new TokenOperatorFactory();
        }

        [Test]
        public void TestConstructorPopulatesAssemblyTypes()
        {
            Assert.IsTrue(factory.Items.Count > 0);
        }

        [Test]
        public void TestPerformOperation()
        {
            var token = new Token { Operation = "ToUpper()" };

            var result = factory.PerformOperation(token, "test");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestPerformOperationWhenInvalidOperationSpecified()
        {
            var token = new Token { Operation = "ToMissingOperation()" };

            Assert.Throws<MissingOperationException>(() => factory.PerformOperation(token, "test"));
        }

        [Test]
        public void TestPerformNoOperation()
        {
            var token = new Token { Operation = null };

            var result = factory.PerformOperation(token, "test");

            Assert.AreEqual("test", result);
        }
    }
}
