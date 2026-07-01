using Tokens.Extensions;
using Xunit;

namespace Tokens.Extensions;

public class ObjectExtensionsPathTests
{
    [Fact]
    public void GivenFlatProperty_WhenSetValue_ThenSetsCorrectly()
    {
        var target = new TestTarget();
        target.SetValue("Name", "Alice");
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenSetValue_ThenStripsTypeAndSets()
    {
        var target = new TestTarget();
        target.SetValue("TestTarget.Name", "Bob");
        Assert.Equal("Bob", target.Name);
    }

    [Fact]
    public void GivenNestedPath_WhenSetValue_ThenCreatesIntermediateAndSets()
    {
        var target = new TestTarget();
        target.SetValue("Inner.Value", "deep");
        Assert.NotNull(target.Inner);
        Assert.Equal("deep", target.Inner!.Value);
    }

    [Fact]
    public void GivenDeeplyNestedPath_WhenSetValue_ThenCreatesAllIntermediates()
    {
        var target = new TestTarget();
        target.SetValue("Inner.Nested.Name", "three-deep");
        Assert.NotNull(target.Inner);
        Assert.NotNull(target.Inner!.Nested);
        Assert.Equal("three-deep", target.Inner.Nested!.Name);
    }

    [Fact]
    public void GivenFlatProperty_WhenGetValue_ThenReturnsCorrectly()
    {
        var target = new TestTarget { Name = "Alice" };
        var result = target.GetValue<string>("Name");
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenGetValue_ThenStripsTypeAndGets()
    {
        var target = new TestTarget { Name = "Bob" };
        var result = target.GetValue<string>("TestTarget.Name");
        Assert.Equal("Bob", result);
    }

    [Fact]
    public void GivenNestedPath_WhenGetValue_ThenTraversesAndGets()
    {
        var target = new TestTarget { Inner = new TestInner { Value = "deep" } };
        var result = target.GetValue<string>("Inner.Value");
        Assert.Equal("deep", result);
    }

    public class TestTarget
    {
        public string? Name { get; set; }
        public TestInner? Inner { get; set; }
    }

    public class TestInner
    {
        public string? Value { get; set; }
        public TestNested? Nested { get; set; }
    }

    public class TestNested
    {
        public string? Name { get; set; }
    }
}
