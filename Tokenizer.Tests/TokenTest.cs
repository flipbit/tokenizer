using System;
using NUnit.Framework;
using Tokens.Extensions;
using Tokens.Validators;

namespace Tokens
{
    [TestFixture]
    public class TokenTest
    {
        private Token token;

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public DateTime Birthday { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            token = new Token();
        }

        [Test]
        public void TestSetTokenValue()
        {
            var person = new Person();

            token.Name = "Person.Name";

            var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual("Sue", person.Name);
        }

        [Test]
        public void TestSetTokenValueWithValidator()
        {
            var person = new Person();

            token.Name = "Person.Age";
            token.Validators.Add(new ValidatorContext(typeof(IsNumeric)));

            var assigned = token.Assign(person, "20", TokenizerOptions.Defaults);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual(20, person.Age);
        }

        [Test]
        public void TestSetTokenValueWithValidatorWhenInvalid()
        {
            var person = new Person();

            token.Name = "Person.Age";
            token.Validators.Add(new ValidatorContext(typeof(IsNumeric)));

            var assigned = token.Assign(person, "Twenty", new TokenizerOptions{ ThrowExceptionOnMissingProperty = false });

            Assert.AreEqual(false, assigned);
            Assert.AreEqual(0, person.Age);
        }

        [Test]
        public void TestSetTokenValueWhenNull()
        {
            var person = new Person();

            token.Name = "Person.Name";

            var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual("Sue", person.Name);
        }
    }
}
