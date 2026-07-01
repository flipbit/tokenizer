using System;
using System.Collections.Generic;
using Xunit;

namespace Tokens.Extensions;

public class ObjectExtensionsTests
{
    private class Foo
    {
        public Bar Bar { get; set; } = null!;

        public string Baz { get; set; } = null!;

        public int? Boo { get; set; }
    }

    private class Bar
    {
        public int Age { get; set; }
    }

    private class GetterOnlyCollectionTarget
    {
        public IList<string> Tags { get; } = new List<string>();
    }

    [Fact]
    public void TestSetPropertyWithClassName()
    {
        var foo = new Foo();

        foo.SetValue("Foo.Baz", "Value");

        Assert.Equal("Value", foo.Baz);
    }

    [Fact]
    public void TestSetPropertyWithoutClassName()
    {
        var foo = new Foo();

        foo.SetValue("Baz", "Value");

        Assert.Equal("Value", foo.Baz);
    }

    [Fact]
    public void TestSetPropertyInitializesChildObjects()
    {
        var foo = new Foo();

        foo.SetValue("Bar.Age", 10);

        Assert.Equal(10, foo.Bar.Age);
    }

    [Fact]
    public void TestSetPropertyIgnoresCase()
    {
        var foo = new Foo();

        foo.SetValue("bar.age", 10, StringComparison.InvariantCultureIgnoreCase);

        Assert.Equal(10, foo.Bar.Age);
    }

    [Fact]
    public void TestSetNullableProperty()
    {
        var foo = new Foo();

        foo.SetValue("Boo", "10");
        //foo.Boo.
        Assert.True(foo.Boo.HasValue);
        Assert.Equal(10, foo.Boo.Value);
    }

    [Fact]
    public void TestGetPropertyWithClassName()
    {
        var foo = new Foo { Baz = "Value" };

        var result = foo.GetValue<string>("Foo.Baz");

        Assert.Equal("Value", result);
    }

    [Fact]
    public void TestGetPropertyWithoutClassName()
    {
        var foo = new Foo { Baz = "Value" };

        var result = foo.GetValue<string>("Baz");

        Assert.Equal("Value", result);
    }

    [Fact]
    public void TestGetPropertyFromChildObject()
    {
        var foo = new Foo { Bar = new Bar { Age = 10 } };

        var result = foo.GetValue<int>("Bar.Age");

        Assert.Equal(10, result);
    }

    [Fact]
    public void TestGetPropertyFromChildObjectIgnoresCase()
    {
        var foo = new Foo { Bar = new Bar { Age = 10 } };

        var result = foo.GetValue<int>("bar.age", StringComparison.InvariantCultureIgnoreCase);

        Assert.Equal(10, result);
    }

    [Fact]
    public void TestGetPropertyWhenNull()
    {
        var foo = new Foo();

        var result = foo.GetValue<string>("Baz");

        Assert.Null(result);
    }

    [Fact]
    public void TestGetPropertyNonGeneric()
    {
        var foo = new Foo { Boo = 5 };

        var result = foo.GetValue("Boo");

        Assert.Equal(5, result);
    }

    [Fact]
    public void GivenGetterOnlyListProperty_WhenSetValue_ThenAddsToExistingCollection()
    {
        // Arrange
        var target = new GetterOnlyCollectionTarget();

        // Act
        target.SetValue("Tags", "first");
        target.SetValue("Tags", "second");

        // Assert
        Assert.Equal(2, target.Tags.Count);
        Assert.Equal("first", target.Tags[0]);
        Assert.Equal("second", target.Tags[1]);
    }
}
