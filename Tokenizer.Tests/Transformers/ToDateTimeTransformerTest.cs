using System;
using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToDateTimeTransformerTest
    {
        private ToDateTimeTransformer @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToDateTimeTransformer();
        }

        [Test]
        public void TestParseDate()
        {
            var result =  (DateTime) @operator.Transform("2014-01-01", "yyyy-MM-dd");

            Assert.AreEqual(new DateTime(2014, 1, 1), result);
            Assert.AreEqual(DateTimeKind.Unspecified, result.Kind);
        }

        [Test]
        public void TestParseDateWithFormat()
        {
            var result = @operator.Transform("2 Mar 2012", "d MMM yyyy");

            Assert.AreEqual(new DateTime(2012, 3, 2), result);
        }

        [Test]
        public void TestParseDateWithNoFormat()
        {
            var result = @operator.Transform("2012-05-06");

            Assert.AreEqual(new DateTime(2012, 5, 6), result);
        }

        [Test]
        public void TestParseDateWithInvalidFormat()
        {
            var result = @operator.Transform("2012-05-06", "dd MMM yy");

            Assert.AreEqual("2012-05-06", result);
        }

        [Test]
        public void TestParseDateWithFormatList()
        {
            var result = @operator.Transform("2012-05-06", "dd MMM yy", "yyyy-MM-dd");

            Assert.AreEqual(new DateTime(2012, 5 ,6), result);
        }

        [Test]
        public void TestParseDateWithEmptyValue()
        {
            var result = @operator.Transform(null);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
