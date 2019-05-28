using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class HintTests
    {
        private Tokenizer tokenizer;

        private class Student
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestOneHintFound()
        {
            const string pattern = @"---
Hint: First Name
---
First Name: {FirstName}";
            const string input = @"First Name: Alice";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.FirstName);

            Assert.AreEqual(1, result.Hints.Matches.Count);
            Assert.AreEqual("First Name", result.Hints.Matches[0].Text);
            Assert.AreEqual(false , result.Hints.Matches[0].Optional);

            Assert.AreEqual(0, result.Hints.Misses.Count);
        }

        [Test]
        public void TestOneHintNotFound()
        {
            const string pattern = @"---
Hint: Last Name
---
First Name: {FirstName}";
            const string input = @"First Name: Alice";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual(null, result.Value.FirstName);

            Assert.AreEqual(0, result.Hints.Matches.Count);

            Assert.AreEqual(1, result.Hints.Misses.Count);
            Assert.AreEqual("Last Name", result.Hints.Misses[0].Text);
            Assert.AreEqual(false , result.Hints.Misses[0].Optional);
        }

        [Test]
        public void TestTwoHintsFound()
        {
            const string pattern = @"---
Hint: First Name
Hint?: Last Name
---
First Name: {FirstName:Trim} Last Name: {LastName}";
            const string input = @"First Name: Alice  Last Name: Smith";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.FirstName);
            Assert.AreEqual("Smith", result.Value.LastName);

            Assert.AreEqual(2, result.Hints.Matches.Count);
            Assert.AreEqual("First Name", result.Hints.Matches[0].Text);
            Assert.AreEqual(false , result.Hints.Matches[0].Optional);
            Assert.AreEqual("Last Name", result.Hints.Matches[1].Text);
            Assert.AreEqual(true , result.Hints.Matches[1].Optional);

            Assert.AreEqual(0, result.Hints.Misses.Count);
        }
        
        [Test]
        public void TestTwoHintsMixed()
        {
            const string pattern = @"---
Hint: First Name
Hint?: Middle Name
---
First Name: {FirstName:Trim} Last Name: {LastName}";
            const string input = @"First Name: Alice  Last Name: Smith";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.FirstName);
            Assert.AreEqual("Smith", result.Value.LastName);

            Assert.AreEqual(1, result.Hints.Matches.Count);
            Assert.AreEqual("First Name", result.Hints.Matches[0].Text);
            Assert.AreEqual(false , result.Hints.Matches[0].Optional);

            Assert.AreEqual(1, result.Hints.Misses.Count);
            Assert.AreEqual("Middle Name", result.Hints.Misses[0].Text);
            Assert.AreEqual(true , result.Hints.Misses[0].Optional);
        }
    }
}

