using NUnit.Framework;
using System.Collections.Generic;

namespace Tokens
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        public void TestHasTagWhenTrue()
        {
            var template = new Template();
            template.Tags.Add("One");

            Assert.IsTrue(template.HasTag("One"));
        }

        [Test]
        public void TestHasTagWhenTrueWhenDifferentCase()
        {
            var template = new Template();
            template.Tags.Add("One");

            Assert.IsTrue(template.HasTag("one"));
        }

        [Test]
        public void TestHasTagWhenTrueWhenMultipleTags()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            Assert.IsTrue(template.HasTag("two"));
        }

        [Test]
        public void TestHasTagWhenMissing()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            Assert.IsFalse(template.HasTag("Four"));
        }

        [Test]
        public void TestHasTagWhenNullInput()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            Assert.IsFalse(template.HasTag(null));
        }

        [Test]
        public void TestHasTagsWhenTrue()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            Assert.True(template.HasTags(new [] { "One", "Two" }));
        }

        [Test]
        public void TestHasTagsWhenTrueAndDifferentCase()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            Assert.True(template.HasTags(new [] { "One", "three" }));
        }

        [Test]
        public void TestHasTagsWhenHasMissingSomeTags()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");
            template.Tags.Add("Four");

            IList<string> missing;
            var hasTags = template.HasTags(new [] { "One", "Five" }, out missing);

            Assert.IsFalse(hasTags);

            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual("Five", missing[0]);
        }

        [Test]
        public void TestHasTagsWhenHasNoTags()
        {
            var template = new Template();

            IList<string> missing;
            var hasTags = template.HasTags(new [] { "One", "three" }, out missing);

            Assert.IsFalse(hasTags);

            Assert.AreEqual(2, missing.Count);
            Assert.AreEqual("One", missing[0]);
            Assert.AreEqual("three", missing[1]);
        }

        [Test]
        public void TestHasTagsWhenHasNullInput()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            IList<string> missing;
            var hasTags = template.HasTags(null, out missing);

            Assert.IsFalse(hasTags);

            Assert.AreEqual(0, missing.Count);
        }

        [Test]
        public void TestHasTagsWhenHasEmptyInput()
        {
            var template = new Template();
            template.Tags.Add("One");
            template.Tags.Add("Two");
            template.Tags.Add("Three");

            IList<string> missing;
            var hasTags = template.HasTags(new string[0], out missing);

            Assert.IsTrue(hasTags);

            Assert.AreEqual(0, missing.Count);
        }

    }
}
