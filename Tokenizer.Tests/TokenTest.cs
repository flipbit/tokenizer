using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class TokenTest
    {
        [Test]
        public void TestTokenContainedIn()
        {
            var token = new Token { Prefix = "<", Suffix = ">" };

            var result = token.ContainedIn("<b>");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainedInWhenNotPresent()
        {
            var token = new Token { Prefix = "<", Suffix = ">" };

            var result = token.ContainedIn("[b]");

            Assert.IsFalse(result);
        }

        [Test]
        public void TestTokenContainedInHasGreedyPrefix()
        {
            var token = new Token { Prefix = "", Suffix = ">" };

            var result = token.ContainedIn("hello > nope");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainedInHasGreedySuffix()
        {
            var token = new Token { Prefix = "<", Suffix = "" };

            var result = token.ContainedIn("hello <nope");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestTokenContainedInIsGreedy()
        {
            var token = new Token { Prefix = "", Suffix = "" };

            var result = token.ContainedIn("hello");

            Assert.IsTrue(result);
        }

        [Test]
        public void TestFunctions()
        {
            var token = new Token { Operation = "IsNumeric() && IsGreater(100)" };

            Assert.AreEqual(2, token.Functions.Count);
            Assert.AreEqual("IsNumeric", token.Functions[0].Name);
            Assert.AreEqual("IsGreater", token.Functions[1].Name);
            Assert.AreEqual("100", token.Functions[1].Parameters[0]);
        }

        [Test]
        public void TestFunctionsWhenEmpty()
        {
            var token = new Token { Operation = "" };

            Assert.AreEqual(0, token.Functions.Count);
        }

        [Test]
        public void TestFunctionsWhenNull()
        {
            var token = new Token();

            Assert.AreEqual(0, token.Functions.Count);
        }
    }
}
