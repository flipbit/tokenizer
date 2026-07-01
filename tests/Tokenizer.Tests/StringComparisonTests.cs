using Xunit;

namespace Tokens
{
    public class StringComparisonTests
    {
        [Theory]
        [InlineData("test", "TEST")]
        [InlineData("Test", "test")]
        public void GivenTemplate_WhenCheckingTagCaseInsensitive_ThenFindsTag(string tagToAdd, string tagToFind)
        {
            var template = new Template("content");
            template.AddTag(tagToAdd);
            Assert.True(template.HasTag(tagToFind));
        }

        [Fact]
        public void GivenTemplate_WhenCheckingNonexistentTag_ThenReturnsFalse()
        {
            var template = new Template("content");
            template.AddTag("existing");
            Assert.False(template.HasTag("nonexistent"));
        }
    }
}
