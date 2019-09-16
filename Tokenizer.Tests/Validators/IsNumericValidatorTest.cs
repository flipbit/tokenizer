using System;
using NUnit.Framework;

namespace Tokens.Validators
{
    [TestFixture]
    public class IsNumericValidatorTest
    {
        private IsNumericValidator validator;

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            validator = new IsNumericValidator();
        }

        [Test]
        public void TestValidateValueWhenNumericInteger()
        {
            var result = validator.IsValid("100");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNumericFloat()
        {
            var result = validator.IsValid("10.0");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidateValueWhenNotNumeric()
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
        public void TestNotValidator()
        {
            var pattern = @"Age: { Age : !IsNumeric }";
            var input = "Age: ten";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual("ten", result.First("Age"));
        }
    }
}
