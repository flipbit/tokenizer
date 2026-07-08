using System.Collections.Immutable;
using Xunit;

#pragma warning disable MA0048 // Scenario test: PropertyPathSetter.Collections.Tests.cs

namespace Tokens.Reflection;

public class PropertyPathSetterCollectionsTests
{
    // ── Shared state ────────────────────────────────────────────────────────────

    private readonly PropertyPathSetter _setter = new();

    // ── Model classes ───────────────────────────────────────────────────────────

    private sealed class CollectionTarget
    {
        public string? Name { get; set; }
        public List<string>? StringList { get; set; }
        public IList<string>? StringIList { get; set; }
        public ICollection<string>? StringICollection { get; set; }
        public string[]? StringArray { get; set; }
        public HashSet<string>? StringHashSet { get; set; }
        public ImmutableList<string>? StringImmutableList { get; set; }
        public ImmutableArray<string> StringImmutableArray { get; set; }
        public List<int>? IntList { get; set; }
        public int[]? IntArray { get; set; }
    }

    private sealed class GetterOnlyCollectionTarget
    {
        public IList<string> Tags { get; } = new List<string>();
    }

    private sealed class GetterOnlyArrayTarget
    {
        public string[] Items { get; } = Array.Empty<string>();
    }

    private sealed class GetterOnlyImmutableTarget
    {
        public ImmutableList<string> Items { get; } = ImmutableList<string>.Empty;
    }

    private sealed class UnsupportedCollectionTarget
    {
        public Dictionary<string, string>? Dict { get; set; }
        public IEnumerable<string>? Enumerable { get; set; }
    }

    // ── List<string> ────────────────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringList_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "alpha", "beta", "gamma" };

        // Act
        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "alpha", "beta", "gamma" }, target.StringList);
    }

    [Fact]
    public void GivenEmptyValues_WhenSetCollectionToStringList_ThenAssignedEmpty()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object>();

        // Act
        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        // Assert
        Assert.NotNull(target.StringList);
        Assert.Empty(target.StringList);
    }

    // ── IList<string> ───────────────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringIList_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "x", "y" };

        // Act
        _setter.SetCollection(target, "StringIList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "x", "y" }, target.StringIList);
    }

    // ── ICollection<string> ─────────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringICollection_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "one", "two" };

        // Act
        _setter.SetCollection(target, "StringICollection", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "one", "two" }, target.StringICollection);
    }

    // ── string[] ────────────────────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringArray_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "c" };

        // Act
        _setter.SetCollection(target, "StringArray", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "a", "b", "c" }, target.StringArray);
    }

    [Fact]
    public void GivenEmptyValues_WhenSetCollectionToStringArray_ThenAssignedEmpty()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object>();

        // Act
        _setter.SetCollection(target, "StringArray", values, StringComparison.Ordinal);

        // Assert
        Assert.NotNull(target.StringArray);
        Assert.Empty(target.StringArray);
    }

    // ── HashSet<string> ─────────────────────────────────────────────────────────

    [Fact]
    public void GivenUniqueStringValues_WhenSetCollectionToStringHashSet_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "p", "q", "r" };

        // Act
        _setter.SetCollection(target, "StringHashSet", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { "p", "q", "r" }, target.StringHashSet);
    }

    [Fact]
    public void GivenDuplicateValues_WhenSetCollectionToStringHashSet_ThenThrowsWithDuplicateMessage()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "a" };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _setter.SetCollection(target, "StringHashSet", values, StringComparison.Ordinal));
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a", ex.Message, StringComparison.Ordinal);
    }

    // ── ImmutableList<string> ───────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringImmutableList_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "i", "ii", "iii" };

        // Act
        _setter.SetCollection(target, "StringImmutableList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(ImmutableList.Create("i", "ii", "iii"), target.StringImmutableList);
    }

    // ── ImmutableArray<string> ──────────────────────────────────────────────────

    [Fact]
    public void GivenStringValues_WhenSetCollectionToStringImmutableArray_ThenAssigned()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "x1", "x2" };

        // Act
        _setter.SetCollection(target, "StringImmutableArray", values, StringComparison.Ordinal);

        // Assert
        Assert.True(target.StringImmutableArray.SequenceEqual(new[] { "x1", "x2" }));
    }

    // ── Element type conversion ──────────────────────────────────────────────────

    [Fact]
    public void GivenStringIntValues_WhenSetCollectionToIntList_ThenConverted()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "1", "2", "3" };

        // Act
        _setter.SetCollection(target, "IntList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, target.IntList);
    }

    [Fact]
    public void GivenStringIntValues_WhenSetCollectionToIntArray_ThenConverted()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "10", "20" };

        // Act
        _setter.SetCollection(target, "IntArray", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { 10, 20 }, target.IntArray);
    }

    // ── Single value ────────────────────────────────────────────────────────────

    [Fact]
    public void GivenSingleValue_WhenSetCollectionToStringList_ThenSingleElementList()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "only" };

        // Act
        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "only" }, target.StringList);
    }

    // ── Getter-only collections ──────────────────────────────────────────────────

    [Fact]
    public void GivenGetterOnlyIList_WhenSetCollection_ThenAddsToExisting()
    {
        // Arrange
        var target = new GetterOnlyCollectionTarget();
        var values = new List<object> { "tag1", "tag2" };

        // Act
        _setter.SetCollection(target, "Tags", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(new[] { "tag1", "tag2" }, target.Tags);
    }

    [Fact]
    public void GivenGetterOnlyArray_WhenSetCollection_ThenThrows()
    {
        // Arrange
        var target = new GetterOnlyArrayTarget();
        var values = new List<object> { "a" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => _setter.SetCollection(target, "Items", values, StringComparison.Ordinal));
    }

    [Fact]
    public void GivenGetterOnlyImmutableList_WhenSetCollection_ThenThrows()
    {
        // Arrange
        var target = new GetterOnlyImmutableTarget();
        var values = new List<object> { "a" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => _setter.SetCollection(target, "Items", values, StringComparison.Ordinal));
    }

    // ── Unsupported types ────────────────────────────────────────────────────────

    [Fact]
    public void GivenDictionaryProp_WhenSetCollection_ThenThrowsWithNotSupportedMessage()
    {
        // Arrange
        var target = new UnsupportedCollectionTarget();
        var values = new List<object> { "v" };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _setter.SetCollection(target, "Dict", values, StringComparison.Ordinal));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenIEnumerableProp_WhenSetCollection_ThenThrowsWithNotSupportedMessage()
    {
        // Arrange
        var target = new UnsupportedCollectionTarget();
        var values = new List<object> { "v" };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => _setter.SetCollection(target, "Enumerable", values, StringComparison.Ordinal));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── IsCollectionProperty ────────────────────────────────────────────────────

    [Fact]
    public void GivenListProp_WhenIsCollectionProperty_ThenTrue()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(CollectionTarget), "StringList", StringComparison.Ordinal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenArrayProp_WhenIsCollectionProperty_ThenTrue()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(CollectionTarget), "StringArray", StringComparison.Ordinal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenHashSetProp_WhenIsCollectionProperty_ThenTrue()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(CollectionTarget), "StringHashSet", StringComparison.Ordinal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenImmutableListProp_WhenIsCollectionProperty_ThenTrue()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(CollectionTarget), "StringImmutableList", StringComparison.Ordinal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenScalarProp_WhenIsCollectionProperty_ThenFalse()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(CollectionTarget), "Name", StringComparison.Ordinal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenDictionaryProp_WhenIsCollectionProperty_ThenFalse()
    {
        // Arrange & Act
        var result = _setter.IsCollectionProperty(typeof(UnsupportedCollectionTarget), "Dict", StringComparison.Ordinal);

        // Assert
        Assert.False(result);
    }
}
