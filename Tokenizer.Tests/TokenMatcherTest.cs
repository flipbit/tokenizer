using System.Runtime.InteropServices.ComTypes;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class TokenMatcherTest
    {
        private TokenMatcher matcher;

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            matcher = new TokenMatcher();
        }

        [Test]
        public void TestParseOnePattern()
        {
            matcher.RegisterTemplate("Name: {Person.Name}", "Person");

            var result = matcher.Match<Person>("Name: Alice");

            var person = result.BestMatch.Result;

            Assert.AreEqual("Alice", person.Name);
        }

        [Test]
        public void TestParseTwoPatterns()
        {
            matcher.RegisterTemplate("Name: {Person.Name}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Result.Name);
            Assert.AreEqual(30, match.Result.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }
    }
}
