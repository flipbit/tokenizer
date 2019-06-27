using System;
using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToDateTimeUtcTransformerTest
    {
        private ToDateTimeUtcTransformer @operator;

        [SetUp]
        public void SetUp()
        {
            @operator = new ToDateTimeUtcTransformer();
        }

        [Test]
        public void TestParseDateSetsKindToUtc()
        {
            var result = @operator.CanTransform("2014-01-01", new [] { "yyyy-MM-dd" }, out var t);
            var dateTime = (DateTime) t;

            Assert.IsTrue(result);
            Assert.AreEqual(new DateTime(2014, 1, 1), t);
            Assert.AreEqual(DateTimeKind.Utc, dateTime.Kind);
        }

        [Test]
        public void TestParseDateAndTime()
        {
            var result = @operator.CanTransform("2014-01-01 10:00:00", new [] { "yyyy-MM-dd hh:mm:ss" }, out var t);
            var dateTime = (DateTime) t;

            Assert.IsTrue(result);
            Assert.AreEqual(new DateTime(2014, 1, 1, 10, 0, 0), dateTime);
            Assert.AreEqual(DateTimeKind.Utc, dateTime.Kind);
        }

        [Test]
        public void TestParseDateAndTimeIsoFormat()
        {
            var result = @operator.CanTransform("2014-01-01T10:00:00Z", new [] { "yyyy-MM-ddThh:mm:ssZ" }, out var t);
            var dateTime = (DateTime) t;

            Assert.IsTrue(result);
            Assert.AreEqual(new DateTime(2014, 1, 1, 10, 0, 0), dateTime);
            Assert.AreEqual(DateTimeKind.Utc, dateTime.Kind);
        }
    }
}
