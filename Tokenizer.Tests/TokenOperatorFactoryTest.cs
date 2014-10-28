using System;
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
            Assert.IsTrue(factory.Operators.Count > 0);
        }

        [Test]
        public void TestTokenContainsOperationWhenTrue()
        {
            var token = new Token { Operation = "ToUpper()" };

            var result = factory.ContainsOperation(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsOperationWhenFalse()
        {
            var token = new Token { Operation = "IsNumeric()" };

            var result = factory.ContainsOperation(token);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestTokenContainsOperationWhenInvalid()
        {
            var token = new Token { Operation = "Huh?" };

            Assert.Throws<InvalidOperationException>(() => factory.ContainsOperation(token));
        }

        [Test]
        public void TestTokenContainsOperationWhenHaveMultipleOperations()
        {
            var token = new Token { Operation = "ToUpper() && ToLower()" };

            var result = factory.ContainsOperation(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsOperationWhenHaveMultipleOperationsAndValidators()
        {
            var token = new Token { Operation = "ToUpper() && IsNumeric()" };

            var result = factory.ContainsOperation(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsValidatorWhenTrue()
        {
            var token = new Token { Operation = "IsNumeric()" };

            var result = factory.ContainsValidator(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsValidatorWhenFalse()
        {
            var token = new Token { Operation = "ToLower()" };

            var result = factory.ContainsValidator(token);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestTokenContainsValidatorWhenInvalid()
        {
            var token = new Token { Operation = "Huh?" };

            Assert.Throws<InvalidOperationException>(() => factory.ContainsValidator(token));
        }

        [Test]
        public void TestTokenContainsValidatorWhenHaveMultipleOperations()
        {
            var token = new Token { Operation = "IsNumeric() && IsDateTime()" };

            var result = factory.ContainsValidator(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsValidatorWhenHaveMultipleOperationsAndValidators()
        {
            var token = new Token { Operation = "ToUpper() && IsNumeric()" };

            var result = factory.ContainsValidator(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainsMissingFunctions()
        {
            var token = new Token { Operation = "ToUpper() && IsNumeric()" };

            var result = factory.HasMissingFunctions(token);

            Assert.IsFalse(result);
        }

        [Test]
        public void TestTokenContainsMissingFunctionsWhenHasMissingFunction()
        {
            var token = new Token { Operation = "ToUpper() && IsNumeric() && ToFabulous()" };

            var result = factory.HasMissingFunctions(token);

            Assert.IsTrue(result);
        }

        [Test]
        public void TestPerformOperation()
        {
            var token = new Token { Operation = "ToUpper()" };

            var result = factory.PerformOperation(token, "test");

            Assert.AreEqual("TEST", result);
        }

        [Test]
        public void TestPerformMultipleOperations()
        {
            var token = new Token { Operation = "ToUpper() && ToLower()" };

            var result = factory.PerformOperation(token, "TEst");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void TestPerformNoOperation()
        {
            var token = new Token { Operation = null };

            var result = factory.PerformOperation(token, "test");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void TestValidateTokenWhenValid()
        {
            var token = new Token { Operation = "IsNumeric()" };

            var result = factory.Validate(token, "1234.5");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateTokenWhenInvalid()
        {
            var token = new Token { Operation = "IsNumeric()" };

            var result = factory.Validate(token, "hello world");

            Assert.IsFalse(result);
        }

    }
}
