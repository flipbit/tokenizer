using System;
using NUnit.Framework;

namespace Tokens.Operators
{
    [TestFixture]
    public class ToDateTimeOperatorTest
    {
        private ToDateTimeOperator @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToDateTimeOperator();
        }

        [Test]
        public void TestParseDate()
        {
            var function = new Function();
            function.Parameters.Add("yyyy-MM-dd");

            var result = @operator.Perform(function, null, "2014-01-01");

            Assert.AreEqual(new DateTime(2014, 1, 1), result);
        }

        [Test]
        public void TestParseDateWithFormat()
        {
            var function = new Function();
            function.Parameters.Add("d MMM yyyy");

            var result = @operator.Perform(function, null, "2 Mar 2012");

            Assert.AreEqual(new DateTime(2012, 3, 2), result);
        }

        [Test]
        public void TestParseDateWithNoFormat()
        {
            var function = new Function();
            
            var result = @operator.Perform(function, null, "2012-05-06");

            Assert.AreEqual(new DateTime(2012, 5, 6), result);
        }

        [Test]
        public void TestParseDateWithInvalidFormat()
        {
            var function = new Function();
            function.Parameters.Add("dd MMM yy");

            var result = @operator.Perform(function, null, "2012-05-06");

            Assert.AreEqual(new DateTime(1, 1, 1), result);
        }

        [Test]
        public void TestParseDateWithEmptyValue()
        {
            var function = new Function();

            var result = @operator.Perform(function, null, null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
