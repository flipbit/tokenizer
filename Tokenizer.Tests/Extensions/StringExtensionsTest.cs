using System.Linq;
using NUnit.Framework;

namespace Tokens.Extensions
{
    [TestFixture]
    public class StringExtensionsTest
    {
        [Test]
        public void TestSubStringEmptyString()
        {
            Assert.AreEqual(string.Empty, string.Empty.SubstringAfterString("c"));
        }

        [Test]
        public void TestSubStringWithNonMatchingString()
        {
            Assert.AreEqual("banana", "banana".SubstringAfterString("c"));
        }


        [Test]
        public void TestSubStringWithMultipleMatchingString()
        {
            Assert.AreEqual("ana", "banana".SubstringAfterAnyString("ban", "b"));
        }

        [Test]
        public void TestSubStringWithMatchingString()
        {
            Assert.AreEqual("ana", "banana".SubstringAfterString("n"));
        }

        [Test]
        public void TestSubStringWithAfterLastString()
        {
            Assert.AreEqual("a", "banana".SubstringAfterLastString("n"));
        }

        [Test]
        public void TestSubStringBeforeWithMatchingString()
        {
            Assert.AreEqual("ba", "banana".SubstringBeforeString("n"));
        }

        [Test]
        public void TestSubStringBeforeWithMatchingLastString()
        {
            Assert.AreEqual("bana", "banana".SubstringBeforeLastString("n"));
        }

        [Test]
        public void TestSplitLines()
        {
            var result = "one\r\ntwo".ToLines().ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("one", result[0]);
            Assert.AreEqual("two", result[1]);
        }

        [Test]
        public void TestSplitWithNewLinesOnly()
        {
            var result = "one\ntwo".ToLines().ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("one", result[0]);
            Assert.AreEqual("two", result[1]);
        }

        [Test]
        public void TestSplitWithCarriageReturnsOnly()
        {
            var result = "one\rtwo".ToLines().ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("one", result[0]);
            Assert.AreEqual("two", result[1]);
        }

        [Test]
        public void TestSplitWithOneLineOnly()
        {
            var result = "one".ToLines().ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("one", result[0]);
        }

        [Test]
        public void TestSplitWithNoLinesOnly()
        {
            var result = string.Empty.ToLines().ToList();

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestSplitWithNullValues()
        {
            var result = ((string) null).ToLines().ToList();

            Assert.AreEqual(0, result.Count);
        }
    }
}