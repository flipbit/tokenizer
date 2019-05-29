using NUnit.Framework;

namespace Tokens.Types
{
    [TestFixture]
    public class EnumTests
    {
        private Tokenizer tokenizer;

        private class Student
        {
            public string Name { get; set; }

            public Grade Grade { get; set; }
        }

        private enum Grade
        {
            GradeA,
            GradeB,
            GradeC,
        }

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestSetEnumValue()
        {
            const string pattern = @"Name: {Name}, Grade: {Grade}";
            const string input = @"Name: Alice, Grade: GradeB";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(Grade.GradeB, result.Value.Grade);
        }

        [Test]
        public void TestSetEnumValueWhenWrongCase()
        {
            const string pattern = @"Name: {Name}, Grade: {Grade}";
            const string input = @"Name: Alice, Grade: Gradec";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(Grade.GradeC, result.Value.Grade);
        }

        [Test]
        public void TestSetEnumValueWhenIncorrectValue()
        {
            const string pattern = @"Name: {Name}, Grade: {Grade}";
            const string input = @"Name: Alice, Grade: GradeE";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.Name);
            Assert.AreEqual(Grade.GradeA, result.Value.Grade);
            Assert.AreEqual(1, result.Exceptions.Count);
        }
    }
}

