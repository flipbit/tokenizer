using System;
using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToDateTimeOperatorTest
    {
        private ToDateTime @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToDateTime();
        }

        [Test]
        public void TestParseDate()
        {
            var result = @operator.Perform("2014-01-01", "yyyy-MM-dd");

            Assert.AreEqual(new DateTime(2014, 1, 1), result);
        }

        [Test]
        public void TestParseDateWithFormat()
        {
            var result = @operator.Perform("2 Mar 2012", "d MMM yyyy");

            Assert.AreEqual(new DateTime(2012, 3, 2), result);
        }

        [Test]
        public void TestParseDateWithNoFormat()
        {
            var result = @operator.Perform("2012-05-06");

            Assert.AreEqual(new DateTime(2012, 5, 6), result);
        }

        [Test]
        public void TestParseDateWithInvalidFormat()
        {
            var result = @operator.Perform("2012-05-06", "dd MMM yy");

            Assert.AreEqual(new DateTime(1, 1, 1), result);
        }

        [Test]
        public void TestParseDateWithEmptyValue()
        {
            var result = @operator.Perform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
