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
            var result = (DateTime) @operator.Transform("2014-01-01", "yyyy-MM-dd");

            Assert.AreEqual(new DateTime(2014, 1, 1), result);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        }
    }
}
