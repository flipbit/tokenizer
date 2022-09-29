using NUnit.Framework;

namespace Tokens.Types
{
    [TestFixture]
    public class BoolTests
    {
        private Tokenizer tokenizer;

        private class Student
        {
            public string Name { get; set; }

            public bool Enrolled { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            LogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestSetBoolValueWhenTrue()
        {
            const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
            const string input = @"Name: Alice, Enrolled: true";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(true, result.Value.Enrolled);
        }

        [Test]
        public void TestSetBoolValueWhenTrueAndUpperCase()
        {
            const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
            const string input = @"Name: Alice, Enrolled: TRUE";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(true, result.Value.Enrolled);
        }

        [Test]
        public void TestSetBoolValueWhenFalse()
        {
            const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
            const string input = @"Name: Alice, Enrolled: False";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(false, result.Value.Enrolled);
        }
    }
}

