using NUnit.Framework;
using NUnit.Framework.Internal;
using Tokens.Extensions;

namespace Tokens
{
    [TestFixture]
    public class ObjectExtensionsTests
    {
        private class Foo
        {
            public Bar Bar { get; set; }

            public string Baz { get; set; }
        }

        public class Bar
        {
            public int Age { get; set; }
        }

        [Test]
        public void TestSetProperty()
        {
            var foo = new Foo();

            foo.SetValue("Foo.Baz", "Value");

            Assert.AreEqual("Value", foo.Baz);
        }
    }
}
