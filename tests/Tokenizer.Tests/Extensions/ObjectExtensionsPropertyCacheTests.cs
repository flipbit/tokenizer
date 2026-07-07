using Xunit;

namespace Tokens.Extensions;

public class ObjectExtensionsPropertyCacheTests
{
    [Fact]
    public void GivenSameType_WhenSettingMultipleProperties_ThenSucceeds()
    {
        var target1 = new TestTarget();
        var target2 = new TestTarget();

        target1.SetValue("Name", "Alice", StringComparison.Ordinal);
        target2.SetValue("Name", "Bob", StringComparison.Ordinal);

        Assert.Equal("Alice", target1.Name);
        Assert.Equal("Bob", target2.Name);
    }

    [Fact]
    public void GivenNestedType_WhenSettingProperty_ThenSucceeds()
    {
        var target = new TestTarget();
        target.SetValue("Inner.Value", "test", StringComparison.Ordinal);

        Assert.NotNull(target.Inner);
        Assert.Equal("test", target.Inner!.Value);
    }

    public class TestTarget
    {
        public string? Name { get; set; }
        public TestInner? Inner { get; set; }
    }

    public class TestInner
    {
        public string? Value { get; set; }
    }
}
