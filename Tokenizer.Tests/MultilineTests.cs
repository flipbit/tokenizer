using System.Collections.Generic;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class MultilineTests
    {
        private Tokenizer tokenizer;

        private class Student
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public List<string> Classes { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestMultilineRepeating()
        {
            const string pattern = @"First Name:
  {FirstName $ }

Classes:
  {Classes $ * }

Last Name:
  {LastName $ }
";
            const string input = @"First Name:
  Alice

Classes:
  French
  History
  Maths

Last Name:
  Smith
";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.FirstName);
            Assert.AreEqual(3, result.Value.Classes.Count);
            Assert.AreEqual("French", result.Value.Classes[0]);
            Assert.AreEqual("History", result.Value.Classes[1]);
            Assert.AreEqual("Maths", result.Value.Classes[2]);
            Assert.AreEqual("Smith", result.Value.LastName);
        }

        [Test]
        public void TestMultilineRepeatingIndented()
        {
            const string pattern = @"    Relevant dates:
        Registered on: {FirstName}
        Expiry date:  {LastName}

    Registration status:
        Registered until expiry date.

    Name servers:
        { Classes $ *}
";
            const string input = @"    Relevant dates:
        Registered on: Alice
        Expiry date:  Smith

    Registration status:
        Registered until expiry date.

    Name servers:
        ns1.rbsov.bbc.co.uk       212.58.241.67
        ns1.tcams.bbc.co.uk       212.72.49.3
        ns1.thdow.bbc.co.uk       212.58.240.163";

            var result = tokenizer.Tokenize<Student>(pattern, input);

            Assert.AreEqual("Alice", result.Value.FirstName);
            Assert.AreEqual(3, result.Value.Classes.Count);
            Assert.AreEqual("ns1.rbsov.bbc.co.uk       212.58.241.67", result.Value.Classes[0]);
            Assert.AreEqual("ns1.tcams.bbc.co.uk       212.72.49.3", result.Value.Classes[1]);
            Assert.AreEqual("ns1.thdow.bbc.co.uk       212.58.240.163", result.Value.Classes[2]);
            Assert.AreEqual("Smith", result.Value.LastName);
        }
    }
}

