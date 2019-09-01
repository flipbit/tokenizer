using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class CandidateTokenListTests
    {
        [Test]
        public void TestAddToken()
        {
            var token = new Token("foo") { Preamble = "bar" };

            var list = new CandidateTokenList();

            list.Add(token);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("bar", list.Preamble);
        }
    }
}
