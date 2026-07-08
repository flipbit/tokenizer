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

    private readonly PropertyPathSetter _setter = new PropertyPathSetter(new TokenizerOptions());

    [Fact]
    public void GivenFlatPath_WhenSetScalar_ThenPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        _setter.SetScalar(person, "Name", "Alice", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenSetScalar_ThenTypePrefixIsStrippedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        _setter.SetScalar(person, "Person.Name", "Bob", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Bob", person.Name);
    }

    [Fact]
    public void GivenNestedPath_WhenSetScalar_ThenIntermediateIsCreatedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        _setter.SetScalar(person, "Address.City", "London", StringComparison.Ordinal);

        // Assert
        Assert.Equal("London", person.Address.City);
    }

    [Fact]
    public void GivenDeeplyNestedPath_WhenSetScalar_ThenAllIntermediatesCreatedAndPropertyIsAssigned()
    {
        // Arrange
        var person = new Person();

        // Act
        _setter.SetScalar(person, "Address.Region.Code", "SW1", StringComparison.Ordinal);

        // Assert
        Assert.Equal("SW1", person.Address.Region.Code);
    }

    [Fact]
    public void GivenCaseInsensitiveComparison_WhenSetScalar_ThenMatchesPropertyCaseInsensitively()
    {
        // Arrange
        var person = new Person();

        // Act
        _setter.SetScalar(person, "name", "Carol", StringComparison.OrdinalIgnoreCase);

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
            _setter.SetScalar(person, "NonExistent", "value", StringComparison.Ordinal));
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
            _setter.SetScalar(person, deepPath, "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenStructIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var obj = new WithStructIntermediate();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

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
            _setter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

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
            _setter.SetScalar(obj, "Mid.Value", "x", StringComparison.Ordinal));

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
            _setter.SetScalar(obj, "ReadOnlyProp", "new value", StringComparison.Ordinal));

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
            _setter.SetScalar(person, null!, "value", StringComparison.Ordinal));
        Assert.Throws<ArgumentNullException>(() =>
            _setter.SetScalar(person, "", "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenExistingIntermediate_WhenSetScalar_ThenExistingIntermediateIsPreserved()
    {
        // Arrange
        var obj = new WithExistingIntermediate();
        var originalAddress = obj.Address;

        // Act
        _setter.SetScalar(obj, "Address.City", "Paris", StringComparison.Ordinal);

        // Assert
        Assert.Same(originalAddress, obj.Address);
        Assert.Equal("Paris", obj.Address.City);
    }

    // ── Temporal auto-conversion ────────────────────────────────────────────────

    private sealed class WithDateTime
    {
        public DateTime Timestamp { get; set; }
    }

    private sealed class WithDateTimeOffset
    {
        public DateTimeOffset Timestamp { get; set; }
    }

    [Fact]
    public void GivenDateTimeOffsetValue_WhenSettingDateTimeProperty_ThenProjectsUtcKind()
    {
        // Arrange
        var obj = new WithDateTime();
        var source = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        _setter.SetScalar(obj, "Timestamp", source, StringComparison.Ordinal);

        // Assert
        Assert.Equal(DateTimeKind.Utc, obj.Timestamp.Kind);
        Assert.Equal(10, obj.Timestamp.Hour);
    }

    [Fact]
    public void GivenDateTimeOffsetValue_WhenSettingDateTimeOffsetProperty_ThenAssignsDirect()
    {
        // Arrange
        var obj = new WithDateTimeOffset();
        var source = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.FromHours(3));

        // Act
        _setter.SetScalar(obj, "Timestamp", source, StringComparison.Ordinal);

        // Assert
        Assert.Equal(source, obj.Timestamp);
    }

    [Fact]
    public void GivenDateString_WhenSettingDateTimeProperty_ThenParsesAndAssigns()
    {
        // Arrange
        var obj = new WithDateTime();

        // Act
        _setter.SetScalar(obj, "Timestamp", "2024-06-15", StringComparison.Ordinal);

        // Assert
        Assert.Equal(2024, obj.Timestamp.Year);
        Assert.Equal(6, obj.Timestamp.Month);
        Assert.Equal(15, obj.Timestamp.Day);
    }

    [Fact]
    public void GivenDateString_WhenSettingDateTimeOffsetProperty_ThenParsesAndAssigns()
    {
        // Arrange
        var obj = new WithDateTimeOffset();

        // Act
        _setter.SetScalar(obj, "Timestamp", "2024-06-15T10:30:00Z", StringComparison.Ordinal);

        // Assert
        Assert.Equal(2024, obj.Timestamp.Year);
        Assert.Equal(10, obj.Timestamp.Hour);
    }

#if NET6_0_OR_GREATER
    private sealed class WithDateOnly
    {
        public DateOnly Date { get; set; }
    }

    [Fact]
    public void GivenDateTimeOffsetValue_WhenSettingDateOnlyProperty_ThenProjectsDate()
    {
        // Arrange
        var obj = new WithDateOnly();
        var source = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        _setter.SetScalar(obj, "Date", source, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateOnly(2024, 6, 15), obj.Date);
    }

    [Fact]
    public void GivenDateString_WhenSettingDateOnlyProperty_ThenParsesAndAssigns()
    {
        // Arrange
        var obj = new WithDateOnly();

        // Act
        _setter.SetScalar(obj, "Date", "2024-06-15", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateOnly(2024, 6, 15), obj.Date);
    }
#endif
}
