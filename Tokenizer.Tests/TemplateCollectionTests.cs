using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class TemplateCollectionTests
    {
        private TemplateCollection collection;

        [SetUp]
        public void SetUp()
        {
            collection = new TemplateCollection();
        }

        [Test]
        public void TestCollectionContainsTagWhenTrue()
        {
            var template = new Template();
            template.Tags.Add("One");

            collection.Add(template);

            Assert.IsTrue(collection.ContainsTag("One"));
        }

        [Test]
        public void TestCollectionContainsTagWhenTrueAndDifferentCase()
        {
            var template = new Template();
            template.Tags.Add("One");

            collection.Add(template);

            Assert.IsTrue(collection.ContainsTag("one"));
        }

        [Test]
        public void TestCollectionContainsTagWhenFalse()
        {
            var template = new Template();
            template.Tags.Add("One");

            collection.Add(template);

            Assert.IsFalse(collection.ContainsTag("two"));
        }

        [Test]
        public void TestCollectionContainsAllTagsWhenTrue()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");

            collection.Add(template);

            Assert.IsTrue(collection.ContainsAllTags("One", "Two"));
        }

        [Test]
        public void TestCollectionContainsAllTagsWhenFalse()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");

            collection.Add(template);

            Assert.IsFalse(collection.ContainsAllTags("One", "Two", "Three"));
        }
    }
}
