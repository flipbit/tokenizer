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

        [Test]
        public void TestKeepCharacters()
        {
            var result = "123456".Keep("123");

            Assert.AreEqual("123", result);
        }

        [Test]
        public void TestKeepCharactersWhenNoneExist()
        {
            var result = "123456".Keep("789");

            Assert.AreEqual("", result);
        }

        [Test]
        public void TestKeepCharactersWhenInputEmpty()
        {
            var result = "".Keep("789");

            Assert.AreEqual("", result);
        }

        [Test]
        public void TestKeepCharactersWhenInputNull()
        {
            var result = ((string) null).Keep("789");

            Assert.AreEqual("", result);
        }

        [Test]
        public void TestKeepCharactersWhenMatchNull()
        {
            var result = "123456".Keep(null);

            Assert.AreEqual("", result);
        }

        [Test]
        public void TestSubstringBeforeNewLineWithUnixNewLine()
        {
            var result = "Hello\nWorld".SubstringBeforeNewLine();

            Assert.AreEqual("Hello", result);
        }

        [Test]
        public void TestSubstringBeforeNewLineWithWindowsNewLine()
        {
            var result = "Hello\r\nWorld".SubstringBeforeNewLine();

            Assert.AreEqual("Hello", result);
        }

        [Test]
        public void TestSubstringBeforeNewLineWitNoNewLine()
        {
            var result = "Hello World".SubstringBeforeNewLine();

            Assert.AreEqual("Hello World", result);
        }

        [Test]
        public void TestSubstringBeforeNewLineWhenEmpty()
        {
            var result = "".SubstringBeforeNewLine();

            Assert.AreEqual("", result);
        }

        [Test]
        public void TestSubstringBeforeNewLineWhenNull()
        {
            var result = ((string) null).SubstringBeforeNewLine();

            Assert.AreEqual(null, result);
        }
    }
}