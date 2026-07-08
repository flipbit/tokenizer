using Xunit;

namespace Tokens.Reflection;

public class PropertyPathSetterTests
{
    // ── Model classes ───────────────────────────────────────────────────────────

    private sealed class Person
    {
        public string Name { get; set; } = null!;
        public Address Address { get; set; } = null!;
    }

    private sealed class Address
    {
        public string City { get; set; } = null!;
        public Region Region { get; set; } = null!;
    }

    private sealed class Region
    {
        public string Code { get; set; } = null!;
    }

    private sealed class WithReadOnly
    {
        public string ReadOnlyProp { get; } = "fixed";
    }

    private interface IIntermediate
    {
        public string Value { get; set; }
    }

    private abstract class AbstractIntermediate
    {
        public string Value { get; set; } = null!;
    }

    private sealed class WithStructIntermediate
    {
        public StructIntermediate Mid { get; set; }
        public string End { get; set; } = null!;
    }

    private struct StructIntermediate
    {
        public string Value { get; set; }
    }

    private sealed class WithInterfaceIntermediate
    {
        public IIntermediate Mid { get; set; } = null!;
    }

    private sealed class WithAbstractIntermediate
    {
        public AbstractIntermediate Mid { get; set; } = null!;
    }

    private sealed class WithExistingIntermediate
    {
        public Address Address { get; set; } = new Address { City = "existing" };
    }

    // ── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public void GivenFlatPath_WhenSetScalar_ThenPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        PropertyPathSetter.SetScalar(person, "Name", "Alice", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenSetScalar_ThenTypePrefixIsStrippedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        PropertyPathSetter.SetScalar(person, "Person.Name", "Bob", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Bob", person.Name);
    }

    [Fact]
    public void GivenNestedPath_WhenSetScalar_ThenIntermediateIsCreatedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        PropertyPathSetter.SetScalar(person, "Address.City", "London", StringComparison.Ordinal);

        // Assert
        Assert.Equal("London", person.Address.City);
    }

    [Fact]
    public void GivenDeeplyNestedPath_WhenSetScalar_ThenAllIntermediatesCreatedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        PropertyPathSetter.SetScalar(person, "Address.Region.Code", "SW1", StringComparison.Ordinal);

        // Assert
        Assert.Equal("SW1", person.Address.Region.Code);
    }

    [Fact]
    public void GivenCaseInsensitiveComparison_WhenSetScalar_ThenMatchesPropertyCaseInsensitively()
    {
        // Arrange
        var person = new Person();

        // Act
        PropertyPathSetter.SetScalar(person, "name", "Carol", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("Carol", person.Name);
    }

    [Fact]
    public void GivenMissingProperty_WhenSetScalar_ThenThrowsMissingMemberException()
    {
        // Arrange
        var person = new Person();

        // Act & Assert
        Assert.Throws<MissingMemberException>(() =>
            PropertyPathSetter.SetScalar(person, "NonExistent", "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDepthExceedsLimit_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var person = new Person();
        // Build a path with 11 segments (exceeds MaxDepth of 10)
        var deepPath = "a.b.c.d.e.f.g.h.i.j.k";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            PropertyPathSetter.SetScalar(person, deepPath, "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenStructIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new WithStructIntermediate();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropertyPathSetter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

        // Assert
        Assert.Contains("Value type", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenInterfaceIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new WithInterfaceIntermediate();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropertyPathSetter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

        // Assert
        Assert.Contains("interface", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenAbstractIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new WithAbstractIntermediate();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropertyPathSetter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

        // Assert
        Assert.Contains("abstract", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenReadOnlyProperty_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new WithReadOnly();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropertyPathSetter.SetScalar(obj, "ReadOnlyProp", "new value", StringComparison.Ordinal));

        // Assert
        Assert.Contains("read-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenNullOrEmptyPath_WhenSetScalar_ThenThrowsArgumentNullException()
    {
        // Arrange
        var person = new Person();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PropertyPathSetter.SetScalar(person, null!, "value", StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>(() =>
            PropertyPathSetter.SetScalar(person, "", "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenExistingIntermediate_WhenSetScalar_ThenExistingIntermediateIsPreserved()
    {
        // Arrange
        var obj = new WithExistingIntermediate();
        var originalAddress = obj.Address;

        // Act
        PropertyPathSetter.SetScalar(obj, "Address.City", "Paris", StringComparison.Ordinal);

        // Assert
        Assert.Same(originalAddress, obj.Address);
        Assert.Equal("Paris", obj.Address.City);
    }
}
