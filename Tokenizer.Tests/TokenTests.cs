using System;
using NUnit.Framework;
using Tokens.Enumerators;
using Tokens.Validators;

namespace Tokens
{
    [TestFixture]
    public class TokenTests
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
            token = new Token("Test");
        }

        [Test]
        public void TestSetTokenValue()
        {
            var person = new Person();

            token.Name = "Person.Name";

            var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults, new FileLocation(), out var value);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual("Sue", person.Name);
        }

        [Test]
        public void TestSetTokenValueWithValidator()
        {
            var person = new Person();

            token.Name = "Person.Age";
            token.Decorators.Add(new TokenDecoratorContext(typeof(IsNumericValidator)));

            var assigned = token.Assign(person, "20", TokenizerOptions.Defaults, new FileLocation(), out var value);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual(20, person.Age);
        }

        [Test]
        public void TestSetTokenValueWithValidatorWhenInvalid()
        {
            var person = new Person();

            token.Name = "Person.Age";
            token.Decorators.Add(new TokenDecoratorContext(typeof(IsNumericValidator)));

            var assigned = token.Assign(person, "Twenty", TokenizerOptions.Defaults, new FileLocation(), out var value);

            Assert.AreEqual(false, assigned);
            Assert.AreEqual(0, person.Age);
        }

        [Test]
        public void TestSetTokenValueWhenNull()
        {
            var person = new Person();

            token.Name = "Person.Name";

            var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults, new FileLocation(), out var value);

            Assert.AreEqual(true, assigned);
            Assert.AreEqual("Sue", person.Name);
        }
    }
}
