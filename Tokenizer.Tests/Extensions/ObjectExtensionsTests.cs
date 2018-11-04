using System;
using NUnit.Framework;

namespace Tokens.Extensions
{
    [TestFixture]
    public class ObjectExtensionsTests
    {
        private class Foo
        {
            public Bar Bar { get; set; }

            public string Baz { get; set; }

            public int? Boo { get; set; }
        }

        public class Bar
        {
            public int Age { get; set; }
        }

        [Test]
        public void TestSetPropertyWithClassName()
        {
            var foo = new Foo();

            foo.SetValue("Foo.Baz", "Value");

            Assert.AreEqual("Value", foo.Baz);
        }

        [Test]
        public void TestSetPropertyWithoutClassName()
        {
            var foo = new Foo();

            foo.SetValue("Baz", "Value");

            Assert.AreEqual("Value", foo.Baz);
        }

        [Test]
        public void TestSetPropertyInitializesChildObjects()
        {
            var foo = new Foo();

            foo.SetValue("Bar.Age", 10);

            Assert.AreEqual(10, foo.Bar.Age);
        }

        [Test]
        public void TestSetPropertyIgnoresCase()
        {
            var foo = new Foo();

            foo.SetValue("bar.age", 10, StringComparison.InvariantCultureIgnoreCase);

            Assert.AreEqual(10, foo.Bar.Age);
        }

        [Test]
        public void TestSetNullableProperty()
        {
            var foo = new Foo();

            foo.SetValue("Boo", "10");
            //foo.Boo.
            Assert.IsTrue(foo.Boo.HasValue);
            Assert.AreEqual(10, foo.Boo.Value);
        }
    }
}
