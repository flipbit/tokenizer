using System.Collections.Generic;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class SplitTests
    {
        private Tokenizer tokenizer;

        private class Foo
        {
            public List<string> Names { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestSplitValue()
        {
            const string pattern = @"Names: { Names : Split(',') }";
            const string input = @"Names: Alice,Bob,Charles";

            var results = tokenizer.Tokenize<Foo>(pattern, input);

            var foo = results.Value;

            Assert.AreEqual(3, foo.Names.Count);
            Assert.AreEqual("Alice", foo.Names[0]);
            Assert.AreEqual("Bob", foo.Names[1]);
            Assert.AreEqual("Charles", foo.Names[2]);
        }
    }
}

