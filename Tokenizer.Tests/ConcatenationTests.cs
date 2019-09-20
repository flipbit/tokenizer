using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Tokens.Extensions;

namespace Tokens
{
    [TestFixture]
    public class ConcatenationTests
    {
        private Tokenizer tokenizer;

        private class Foo
        {
            public string Name { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestConcatTwoValues()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual(1, result.Matches.Count);

            Assert.AreEqual("AliceBob", result.First("Name"));
        }

        [Test]
        public void TestConcatTwoValuesWithReflectedObject()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize<Foo>(pattern, input);

            Assert.AreEqual(1, result.Tokens.Matches.Count);

            Assert.AreEqual("AliceBob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
            Assert.AreEqual("AliceBob", result.Value.Name);
        }

        [Test]
        public void TestConcatTwoValuesWithSeparator()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual(1, result.Matches.Count);

            Assert.AreEqual("Alice, Bob", result.First("Name"));
        }

        [Test]
        public void TestConcatTwoValuesWithReflectedObjectWithSeparator()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize<Foo>(pattern, input);

            Assert.AreEqual(1, result.Tokens.Matches.Count);

            Assert.AreEqual("Alice, Bob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
            Assert.AreEqual("Alice, Bob", result.Value.Name);
        }

        [Test]
        public void TestConcatTwoValuesWithNewLineSeparator()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual(1, result.Matches.Count);

            Assert.AreEqual($"Alice{Environment.NewLine}Bob", result.First("Name"));
        }

        [Test]
        public void TestConcatTwoValuesWithReflectedObjectWithNewLineSeparator()
        {
            const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
            const string input = @"Name: Alice, Name: Bob";

            var result = tokenizer.Tokenize<Foo>(pattern, input);

            Assert.AreEqual(1, result.Tokens.Matches.Count);

            Assert.AreEqual($"Alice{Environment.NewLine}Bob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
            Assert.AreEqual($"Alice{Environment.NewLine}Bob", result.Value.Name);
        }
    }
}

