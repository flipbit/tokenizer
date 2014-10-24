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
    }
}
