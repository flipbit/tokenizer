﻿using NUnit.Framework;
using Tokens.Parsers;
using Tokens.Transformers;

namespace Tokens
{
    [TestFixture]
    public class TokenMatcherTests
    {
        private TokenMatcher matcher;
        private TokenParser parser;

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            LogConfig.Init();

            matcher = new TokenMatcher();
            parser = new TokenParser();
        }

        [Test]
        public void TestParseOnePattern()
        {
            matcher.RegisterTemplate("Name: {Person.Name}", "Person");

            var result = matcher.Match<Person>("Name: Alice");

            var person = result.BestMatch.Value;

            Assert.AreEqual("Alice", person.Name);
        }

        [Test]
        public void TestParseTwoPatterns()
        {
            matcher.RegisterTemplate("Name: {Person.Name}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(30, match.Value.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestMatchWithHint()
        {
            var template1 = parser.Parse("Name: {Person.Name: SubstringBefore(',') }", "no-age");
            var template2 = parser.Parse("Name: {Person.Name}, Age: {Person.Age}", "with-age");
            template1.Hints.Add(new Hint { Text = "Name", Optional = false });

            matcher.Templates.Add(template1);
            matcher.Templates.Add(template2);

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(0, match.Value.Age);
            Assert.AreEqual("no-age", match.Template.Name);
        }
 
        [Test]
        public void TestMatchWithMultipleHints()
        {
            var template1 = parser.Parse("Name: {Person.Name: SubstringBefore(',') }", "no-age");
            var template2 = parser.Parse("Name: {Person.Name}, Age: {Person.Age}", "with-age");
            template1.Hints.Add(new Hint { Text = "Name", Optional = false });
            template2.Hints.Add(new Hint { Text = "Name", Optional = false });
            template2.Hints.Add(new Hint { Text = "Age", Optional = false });

            matcher.Templates.Add(template1);
            matcher.Templates.Add(template2);

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(30, match.Value.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsContinuesOnError()
        {
            matcher.RegisterTransformer<BlowsUpTransformer>();

            matcher.RegisterTemplate("Name: {Person.Name:BlowsUp}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(30, match.Value.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsNeedsAllRequiredTokens()
        {
            matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}, Location: {Location!}", "with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            Assert.IsTrue(result.Success);

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(0, match.Value.Age);
            Assert.AreEqual("no-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsWithTags()
        {
            matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            matcher.Templates.Get("no-age").Tags.Add("no-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30",  new [] { "no-age" });

            Assert.IsTrue(result.Success);

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(0, match.Value.Age);
            Assert.AreEqual("no-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsWithNoMatchingTags()
        {
            matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            matcher.Templates.Get("no-age").Tags.Add("no-age");
            matcher.Templates.Get("with-age").Tags.Add("with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30", new [] { "Foo" });

            Assert.IsFalse(result.Success);
            Assert.IsNull(result.BestMatch);
        }
        [Test]
        public void TestParseTwoPatternsWithNoTagInput()
        {
            matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            matcher.Templates.Get("no-age").Tags.Add("no-age");
            matcher.Templates.Get("with-age").Tags.Add("with-age");

            var result = matcher.Match<Person>("Name: Alice, Age: 30");

            var match = result.BestMatch;

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(30, match.Value.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsWithTagsSelectsBestMatch()
        {
            matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
            matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

            matcher.Templates.Get("no-age").Tags.Add("no-age");
            matcher.Templates.Get("no-age").Tags.Add("person");
            matcher.Templates.Get("with-age").Tags.Add("with-age");
            matcher.Templates.Get("with-age").Tags.Add("person");

            var result = matcher.Match<Person>("Name: Alice, Age: 30",  new [] { "person" });

            Assert.IsTrue(result.Success);

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.Value.Name);
            Assert.AreEqual(30, match.Value.Age);
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestParseTwoPatternsWithTagsSelectsBestMatchWithNoTags()
        {
            matcher.RegisterTemplate("Name: { Name $ }", "with-name");
            matcher.RegisterTemplate("Name: { Name $ }Age: { Age $ }", "with-age");
            matcher.RegisterTemplate("Name: { Name $ }Age: { Age $ }Location { Location $ }", "with-location");

            var result = matcher.Match("Name: Alice\nAge: 30");

            Assert.IsTrue(result.Success);

            var match = result.BestMatch;

            Assert.AreEqual("Alice", match.First("Name"));
            Assert.AreEqual("30", match.First("Age"));
            Assert.AreEqual("with-age", match.Template.Name);
        }

        [Test]
        public void TestDocumentationTags1()
        {
            var template1 = @"---
name: template1
tag: standard
outOfOrder: true
terminateOnNewLine: true
---
Name: {Name}
Age: {Age}";

            var template2 = @"---
name: template2
tag: extended
outOfOrder: true
terminateOnNewLine: true
---
Name: {Name}
Age: {Age}
Address: {Address}";

            matcher.RegisterTemplate(template1);
            matcher.RegisterTemplate(template2);

            var input = @"Name: Alice
Age: 30
Address: London";


            var result = matcher.Match(input, new[] { "standard" });

            var match = result.BestMatch;

            Assert.AreEqual("template1", match.Template.Name);
            Assert.AreEqual("Alice", match.First("Name"));
            Assert.AreEqual("30", match.First("Age"));
        }
    }
}
