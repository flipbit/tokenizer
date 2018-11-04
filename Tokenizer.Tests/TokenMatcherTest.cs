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
            matcher.AddPattern("Name: {Person.Name}");

            var person = matcher.Match<Person>("Name: Alice");

            Assert.AreEqual("Alice", person.Name);
        }

        [Test]
        public void TestParseTwoPatterns()
        {
            matcher.AddPattern("Name: {Person.Name}", "no-age");
            matcher.AddPattern("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            var matched = matcher.TryMatch<Person>("Name: Alice, Age: 30", out var match);

            Assert.AreEqual(true, matched);
            Assert.AreEqual("Alice", match.Result.Name);
            Assert.AreEqual(30, match.Result.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }
    }
}
