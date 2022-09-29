using System;
using NUnit.Framework;

namespace Tokens.Transformers
{
    [TestFixture]
    public class ToDateTimeUtcTransformerTests
    {
        private ToDateTimeUtcTransformer @operator;

        [SetUp]
        public void SetUp()
        {
            LogConfig.Init();

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

        [Test]
        public void TestTrimUtcDescription()
        {
            var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
            var input = "Date: 2000-01-01 UTC";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First("Date"));
        }

        [Test]
        public void TestTrimUtcDescriptionInBrackets()
        {
            var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
            var input = "Date: 2000-01-01 (UTC)";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First("Date"));
        }

        [Test]
        public void TestWrongFormat()
        {
            var pattern = @"Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }";
            var input = "Date: 2000-1-1 (UTC)";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.IsFalse(result.Contains("Date"));
        }

        [Test]
        public void TestMultipleTokenMultipleFormats()
        {
            var pattern = @"---
# End tokens on new lines
outOfOrder: true

# End tokens on new lines
terminateOnNewLine: true
---
Date: { Date : ToDateTimeUtc('yyyy-MM-dd') }
Date: { Date : ToDateTimeUtc('yyyy-M-d') }";
            var input = "Date: 2000-1-1 (UTC)";

            var result = new Tokenizer().Tokenize(pattern, input);

            Assert.AreEqual(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.First<DateTime>("Date"));
        }
    }
}
