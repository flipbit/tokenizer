# PropertyPathSetter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the monolithic `ObjectExtensions` class with a focused `PropertyPathSetter` that supports all scalar types, collection types, and validates unsupported scenarios with clear exceptions.

**Architecture:** New `PropertyPathSetter` class in `Tokens.Reflection` namespace with three public methods (`SetScalar`, `SetCollection`, `IsCollectionProperty`). `TokenizeResult.Assign<T>()` is rewritten to group matches by property name, route to scalar vs collection assignment, and handle `IgnoreMissingProperties`. `ObjectExtensions` is deleted entirely.

**Tech Stack:** C#, .NET Standard 2.0 / .NET 8.0 / .NET 10.0 multi-targeting, xUnit, reflection (`System.Reflection`)

---

### Task 1: Update CLAUDE.md with test naming convention

**Files:**
- Modify: `CLAUDE.md:112-119`

- [ ] **Step 1: Add test file naming convention to CLAUDE.md**

Add the file naming rule after the existing "Logging in tests" line in the Testing Conventions section:

```markdown
- **File naming**: Test file matches production class: `{ClassName}Tests.cs`. If a single test fixture is too crowded, split into `{ClassName}.{Scenario}.Tests.cs`
```

- [ ] **Step 2: Verify the change**

Run: `grep -A2 "File naming" CLAUDE.md`
Expected: The new line appears in the Testing Conventions section.

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add test file naming convention to CLAUDE.md"
```

---

### Task 2: Create PropertyPathSetter with core path traversal and Tier 1 tests

**Files:**
- Create: `src/Tokenizer/Reflection/PropertyPathSetter.cs`
- Create: `tests/Tokenizer.Tests/Reflection/PropertyPathSetterTests.cs`

This task implements the public API (`SetScalar`, `SetCollection`, `IsCollectionProperty`), path parsing, `FindProperty`, `TraverseToLeaf`, depth limiting, and unsupported intermediate detection. `AssignScalar` handles only the pass-through case (value already correct type) and string-to-string. `AssignCollection` and `ConvertValue` are stubbed to throw `NotImplementedException` — they are implemented in Tasks 3 and 4.

- [ ] **Step 1: Write the Tier 1 test file with all path traversal tests**

Create `tests/Tokenizer.Tests/Reflection/PropertyPathSetterTests.cs` with the following test fixture. Test model classes are private nested classes within the test class.

```csharp
using System.Reflection;
using Tokens.Reflection;
using Xunit;

namespace Tokens.Reflection;

public class PropertyPathSetterTests
{
    private readonly PropertyPathSetter _setter = new();

    [Fact]
    public void GivenFlatProperty_WhenSetScalar_ThenSetsValue()
    {
        // Arrange
        var target = new FlatTarget();

        // Act
        _setter.SetScalar(target, "Name", "Alice", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenTypePrefixedPath_WhenSetScalar_ThenStripsTypeAndSets()
    {
        // Arrange
        var target = new FlatTarget();

        // Act
        _setter.SetScalar(target, "FlatTarget.Name", "Bob", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Bob", target.Name);
    }

    [Fact]
    public void GivenNestedPath_WhenSetScalar_ThenCreatesIntermediateAndSets()
    {
        // Arrange
        var target = new NestedTarget();

        // Act
        _setter.SetScalar(target, "Inner.Value", "deep", StringComparison.Ordinal);

        // Assert
        Assert.NotNull(target.Inner);
        Assert.Equal("deep", target.Inner!.Value);
    }

    [Fact]
    public void GivenDeeplyNestedPath_WhenSetScalar_ThenCreatesAllIntermediates()
    {
        // Arrange
        var target = new NestedTarget();

        // Act
        _setter.SetScalar(target, "Inner.Nested.Name", "three-deep", StringComparison.Ordinal);

        // Assert
        Assert.NotNull(target.Inner);
        Assert.NotNull(target.Inner!.Nested);
        Assert.Equal("three-deep", target.Inner.Nested!.Name);
    }

    [Fact]
    public void GivenCaseInsensitiveComparison_WhenSetScalar_ThenMatchesProperty()
    {
        // Arrange
        var target = new FlatTarget();

        // Act
        _setter.SetScalar(target, "name", "Alice", StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenMissingProperty_WhenSetScalar_ThenThrowsMissingMemberException()
    {
        // Arrange
        var target = new FlatTarget();

        // Act & Assert
        var ex = Assert.Throws<MissingMemberException>(() =>
            _setter.SetScalar(target, "NonExistent", "value", StringComparison.Ordinal));
        Assert.Contains("NonExistent", ex.Message);
        Assert.Contains("FlatTarget", ex.Message);
    }

    [Fact]
    public void GivenDepthExceedsLimit_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var target = new FlatTarget();
        var deepPath = string.Join(".", Enumerable.Range(0, 11).Select(i => "Segment"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(target, deepPath, "value", StringComparison.Ordinal));
        Assert.Contains("maximum depth", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenStructIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var target = new StructIntermediateTarget();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(target, "Position.X", 1, StringComparison.Ordinal));
        Assert.Contains("Value type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenInterfaceIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var target = new InterfaceIntermediateTarget();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(target, "Service.Name", "value", StringComparison.Ordinal));
        Assert.Contains("interface", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenAbstractIntermediate_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var target = new AbstractIntermediateTarget();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(target, "Base.Name", "value", StringComparison.Ordinal));
        Assert.Contains("abstract", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenReadOnlyProperty_WhenSetScalar_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var target = new ReadOnlyTarget();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetScalar(target, "Id", "value", StringComparison.Ordinal));
        Assert.Contains("read-only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenNullPath_WhenSetScalar_ThenThrowsArgumentNullException()
    {
        // Arrange
        var target = new FlatTarget();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _setter.SetScalar(target, null!, "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenEmptyPath_WhenSetScalar_ThenThrowsArgumentNullException()
    {
        // Arrange
        var target = new FlatTarget();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _setter.SetScalar(target, "", "value", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenExistingIntermediate_WhenSetScalar_ThenPreservesExistingObject()
    {
        // Arrange
        var inner = new InnerObject { Value = "original" };
        var target = new NestedTarget { Inner = inner };

        // Act
        _setter.SetScalar(target, "Inner.Value", "updated", StringComparison.Ordinal);

        // Assert
        Assert.Same(inner, target.Inner);
        Assert.Equal("updated", target.Inner!.Value);
    }

    // --- Test model classes ---

    private sealed class FlatTarget
    {
        public string? Name { get; set; }
    }

    private sealed class NestedTarget
    {
        public InnerObject? Inner { get; set; }
    }

    internal sealed class InnerObject
    {
        public string? Value { get; set; }
        public NestedObject? Nested { get; set; }
    }

    internal sealed class NestedObject
    {
        public string? Name { get; set; }
    }

    private sealed class ReadOnlyTarget
    {
        public string Id { get; } = "fixed";
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class StructIntermediateTarget
    {
        public Point Position { get; set; }
    }

    private interface IService
    {
        string Name { get; }
    }

    private sealed class InterfaceIntermediateTarget
    {
        public IService? Service { get; set; }
    }

    private abstract class AbstractBase
    {
        public string? Name { get; set; }
    }

    private sealed class AbstractIntermediateTarget
    {
        public AbstractBase? Base { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PropertyPathSetterTests" --no-build 2>&1 | tail -5`
Expected: Build failure — `PropertyPathSetter` class does not exist yet.

- [ ] **Step 3: Create PropertyPathSetter with path traversal, FindProperty, TraverseToLeaf**

Create `src/Tokenizer/Reflection/PropertyPathSetter.cs`:

```csharp
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Tokens.Exceptions;

namespace Tokens.Reflection;

/// <summary>
/// Reflects values onto .NET objects via dot-separated property paths.
/// Supports scalar assignment, batch collection assignment, and type conversion.
/// </summary>
internal sealed class PropertyPathSetter
{
    private const int MaxDepth = 10;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, string[]> PathSegmentCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Sets a single scalar value at the given dot-separated property path.
    /// </summary>
    public void SetScalar(object target, string path, object value, StringComparison comparison)
    {
        var (leafTarget, leafProperty) = ResolveLeaf(target, path, comparison);
        AssignScalar(leafTarget, leafProperty, value);
    }

    /// <summary>
    /// Sets a collection of values at the given dot-separated property path.
    /// The target property type determines the collection kind (List, array, HashSet, etc.).
    /// </summary>
    public void SetCollection(object target, string path, IReadOnlyList<object> values, StringComparison comparison)
    {
        var (leafTarget, leafProperty) = ResolveLeaf(target, path, comparison);
        AssignCollection(leafTarget, leafProperty, values);
    }

    /// <summary>
    /// Resolves the leaf property at the given path and checks whether it is a supported collection type.
    /// </summary>
    public bool IsCollectionProperty(Type targetType, string path, StringComparison comparison)
    {
        var segments = ParseSegments(path);
        var startDepth = StripTypePrefix(targetType, segments, comparison);

        var currentType = targetType;
        for (var depth = startDepth; depth < segments.Length; depth++)
        {
            var property = FindProperty(currentType, segments[depth], comparison);
            if (depth == segments.Length - 1)
            {
                return IsCollectionType(property.PropertyType);
            }
            currentType = property.PropertyType;
        }

        return false;
    }

    private (object LeafTarget, PropertyInfo LeafProperty) ResolveLeaf(object target, string path, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        var segments = ParseSegments(path);
        var startDepth = StripTypePrefix(target.GetType(), segments, comparison);

        if (segments.Length - startDepth > MaxDepth)
        {
            throw new InvalidOperationException(
                $"Property path '{path}' exceeds maximum depth of {MaxDepth}.");
        }

        return TraverseToLeaf(target, segments, startDepth, comparison);
    }

    private (object LeafTarget, PropertyInfo LeafProperty) TraverseToLeaf(
        object target, string[] segments, int startDepth, StringComparison comparison)
    {
        var current = target;

        for (var depth = startDepth; depth < segments.Length; depth++)
        {
            var property = FindProperty(current.GetType(), segments[depth], comparison);

            if (depth == segments.Length - 1)
            {
                return (current, property);
            }

            ValidateIntermediateType(property);

            var next = property.GetValue(current);
            if (next == null)
            {
                next = Activator.CreateInstance(property.PropertyType)
                    ?? throw new InvalidOperationException(
                        $"Failed to create instance of {property.PropertyType.Name}.");
                property.SetValue(current, next);
            }

            current = next;
        }

        throw new InvalidOperationException($"Failed to traverse property path.");
    }

    private static void ValidateIntermediateType(PropertyInfo property)
    {
        var type = property.PropertyType;

        if (type.IsValueType)
        {
            throw new InvalidOperationException(
                $"Value types cannot be used as intermediate path segments: " +
                $"property '{property.Name}' is type '{type.Name}'. " +
                $"Mutations to value type intermediates would not propagate to the parent object.");
        }

        if (type.IsInterface)
        {
            throw new InvalidOperationException(
                $"Cannot auto-instantiate interface type '{type.Name}' " +
                $"for intermediate property '{property.Name}'.");
        }

        if (type.IsAbstract)
        {
            throw new InvalidOperationException(
                $"Cannot auto-instantiate abstract type '{type.Name}' " +
                $"for intermediate property '{property.Name}'.");
        }
    }

    private void AssignScalar(object target, PropertyInfo property, object value)
    {
        if (!property.CanWrite || property.GetSetMethod() == null)
        {
            throw new InvalidOperationException(
                $"Cannot set property '{property.Name}' on type '{target.GetType().Name}': " +
                $"property is read-only.");
        }

        var converted = ConvertValue(value, property.PropertyType);
        property.SetValue(target, converted);
    }

    private void AssignCollection(object target, PropertyInfo property, IReadOnlyList<object> values)
    {
        // Implemented in Task 4
        throw new NotImplementedException("Collection assignment not yet implemented.");
    }

    private static object ConvertValue(object value, Type targetType)
    {
        // Pass-through: value is already the correct type
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // Full conversion implemented in Task 3
        throw new NotImplementedException("Type conversion not yet implemented.");
    }

    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;

        if (!type.IsGenericType) return false;

        var genericDef = type.GetGenericTypeDefinition();

        if (genericDef == typeof(List<>) ||
            genericDef == typeof(IList<>) ||
            genericDef == typeof(ICollection<>) ||
            genericDef == typeof(HashSet<>))
        {
            return true;
        }

        // Detect immutable collections by type name to avoid hard dependency
        var fullName = genericDef.FullName;
        if (fullName == "System.Collections.Immutable.ImmutableList`1" ||
            fullName == "System.Collections.Immutable.ImmutableArray`1")
        {
            return true;
        }

        return false;
    }

    private static PropertyInfo FindProperty(Type type, string segment, StringComparison comparison)
    {
        var properties = PropertyCache.GetOrAdd(type, static t => t.GetProperties());

        foreach (var property in properties)
        {
            if (string.Equals(property.Name, segment, comparison))
            {
                return property;
            }
        }

        throw new MissingMemberException(
            $"Could not find property '{segment}' on {type.Name}");
    }

    private static string[] ParseSegments(string path)
    {
        return PathSegmentCache.GetOrAdd(path, static p => p.Split('.'));
    }

    private static int StripTypePrefix(Type targetType, string[] segments, StringComparison comparison)
    {
        return string.Equals(targetType.Name, segments[0], comparison) ? 1 : 0;
    }
}
```

- [ ] **Step 4: Build and run the Tier 1 tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PropertyPathSetterTests" -v minimal`
Expected: All 13 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Reflection/PropertyPathSetter.cs tests/Tokenizer.Tests/Reflection/PropertyPathSetterTests.cs
git commit -m "feat: add PropertyPathSetter with core path traversal and Tier 1 tests"
```

---

### Task 3: Implement ConvertValue with exhaustive scalar type conversion and Tier 2 tests

**Files:**
- Modify: `src/Tokenizer/Reflection/PropertyPathSetter.cs`
- Create: `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.ScalarTypes.Tests.cs`

This task replaces the `ConvertValue` stub with the full conversion chain: already-correct-type pass-through, enum, nullable, non-IConvertible structs (Guid, TimeSpan, DateTimeOffset, DateOnly, TimeOnly), and IConvertible primitives.

- [ ] **Step 1: Write the Tier 2 test file**

Create `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.ScalarTypes.Tests.cs`:

```csharp
using Tokens.Exceptions;
using Tokens.Reflection;
using Xunit;

namespace Tokens.Reflection;

public class PropertyPathSetterScalarTypesTests
{
    private readonly PropertyPathSetter _setter = new();

    // --- IConvertible primitives ---

    [Fact]
    public void GivenStringProperty_WhenSetScalar_ThenSetsValue()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "StringProp", "hello", StringComparison.Ordinal);
        Assert.Equal("hello", target.StringProp);
    }

    [Fact]
    public void GivenBoolProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "BoolProp", "true", StringComparison.Ordinal);
        Assert.True(target.BoolProp);
    }

    [Fact]
    public void GivenCharProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "CharProp", "A", StringComparison.Ordinal);
        Assert.Equal('A', target.CharProp);
    }

    [Fact]
    public void GivenByteProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "ByteProp", "255", StringComparison.Ordinal);
        Assert.Equal((byte)255, target.ByteProp);
    }

    [Fact]
    public void GivenSByteProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "SByteProp", "-128", StringComparison.Ordinal);
        Assert.Equal((sbyte)-128, target.SByteProp);
    }

    [Fact]
    public void GivenInt16Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "Int16Prop", "32767", StringComparison.Ordinal);
        Assert.Equal((short)32767, target.Int16Prop);
    }

    [Fact]
    public void GivenUInt16Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "UInt16Prop", "65535", StringComparison.Ordinal);
        Assert.Equal((ushort)65535, target.UInt16Prop);
    }

    [Fact]
    public void GivenInt32Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "Int32Prop", "42", StringComparison.Ordinal);
        Assert.Equal(42, target.Int32Prop);
    }

    [Fact]
    public void GivenUInt32Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "UInt32Prop", "4294967295", StringComparison.Ordinal);
        Assert.Equal(4294967295U, target.UInt32Prop);
    }

    [Fact]
    public void GivenInt64Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "Int64Prop", "9223372036854775807", StringComparison.Ordinal);
        Assert.Equal(long.MaxValue, target.Int64Prop);
    }

    [Fact]
    public void GivenUInt64Property_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "UInt64Prop", "18446744073709551615", StringComparison.Ordinal);
        Assert.Equal(ulong.MaxValue, target.UInt64Prop);
    }

    [Fact]
    public void GivenFloatProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "FloatProp", "3.14", StringComparison.Ordinal);
        Assert.Equal(3.14f, target.FloatProp, precision: 2);
    }

    [Fact]
    public void GivenDoubleProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "DoubleProp", "3.14159", StringComparison.Ordinal);
        Assert.Equal(3.14159, target.DoubleProp, precision: 5);
    }

    [Fact]
    public void GivenDecimalProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "DecimalProp", "99.99", StringComparison.Ordinal);
        Assert.Equal(99.99m, target.DecimalProp);
    }

    [Fact]
    public void GivenDateTimeProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "DateTimeProp", "2026-07-08", StringComparison.Ordinal);
        Assert.Equal(new DateTime(2026, 7, 8), target.DateTimeProp);
    }

    // --- Enum ---

    [Fact]
    public void GivenEnumProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "ColorProp", "Green", StringComparison.Ordinal);
        Assert.Equal(Color.Green, target.ColorProp);
    }

    [Fact]
    public void GivenEnumProperty_WhenSetScalarCaseInsensitive_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "ColorProp", "green", StringComparison.Ordinal);
        Assert.Equal(Color.Green, target.ColorProp);
    }

    [Fact]
    public void GivenEnumProperty_WhenSetScalarInvalidValue_ThenThrows()
    {
        var target = new AllTypesTarget();
        Assert.ThrowsAny<Exception>(() =>
            _setter.SetScalar(target, "ColorProp", "Purple", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenEnumPropertyAlreadyTyped_WhenSetScalar_ThenPassesThrough()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "ColorProp", Color.Blue, StringComparison.Ordinal);
        Assert.Equal(Color.Blue, target.ColorProp);
    }

    // --- Non-IConvertible structs ---

    [Fact]
    public void GivenGuidProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        var guid = Guid.NewGuid();
        _setter.SetScalar(target, "GuidProp", guid.ToString(), StringComparison.Ordinal);
        Assert.Equal(guid, target.GuidProp);
    }

    [Fact]
    public void GivenTimeSpanProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "TimeSpanProp", "01:30:00", StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(90), target.TimeSpanProp);
    }

    [Fact]
    public void GivenDateTimeOffsetProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "DateTimeOffsetProp", "2026-07-08T12:00:00+02:00", StringComparison.Ordinal);
        Assert.Equal(new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.FromHours(2)), target.DateTimeOffsetProp);
    }

    // --- .NET 6+ types ---

    [Fact]
    public void GivenDateOnlyProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "DateOnlyProp", "2026-07-08", StringComparison.Ordinal);
        Assert.Equal(new DateOnly(2026, 7, 8), target.DateOnlyProp);
    }

    [Fact]
    public void GivenTimeOnlyProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "TimeOnlyProp", "14:30:00", StringComparison.Ordinal);
        Assert.Equal(new TimeOnly(14, 30, 0), target.TimeOnlyProp);
    }

    // --- Nullable<T> ---

    [Fact]
    public void GivenNullableIntProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "IntProp", "42", StringComparison.Ordinal);
        Assert.Equal(42, target.IntProp);
    }

    [Fact]
    public void GivenNullableBoolProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "BoolProp", "true", StringComparison.Ordinal);
        Assert.True(target.BoolProp);
    }

    [Fact]
    public void GivenNullableDateTimeProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "DateTimeProp", "2026-07-08", StringComparison.Ordinal);
        Assert.Equal(new DateTime(2026, 7, 8), target.DateTimeProp);
    }

    [Fact]
    public void GivenNullableGuidProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        var guid = Guid.NewGuid();
        _setter.SetScalar(target, "GuidProp", guid.ToString(), StringComparison.Ordinal);
        Assert.Equal(guid, target.GuidProp);
    }

    [Fact]
    public void GivenNullableEnumProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "ColorProp", "Red", StringComparison.Ordinal);
        Assert.Equal(Color.Red, target.ColorProp);
    }

    [Fact]
    public void GivenNullableDecimalProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "DecimalProp", "123.45", StringComparison.Ordinal);
        Assert.Equal(123.45m, target.DecimalProp);
    }

    [Fact]
    public void GivenNullableDateOnlyProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "DateOnlyProp", "2026-07-08", StringComparison.Ordinal);
        Assert.Equal(new DateOnly(2026, 7, 8), target.DateOnlyProp);
    }

    [Fact]
    public void GivenNullableTimeOnlyProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "TimeOnlyProp", "09:15:00", StringComparison.Ordinal);
        Assert.Equal(new TimeOnly(9, 15, 0), target.TimeOnlyProp);
    }

    [Fact]
    public void GivenNullableTimeSpanProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "TimeSpanProp", "02:30:00", StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromHours(2.5), target.TimeSpanProp);
    }

    [Fact]
    public void GivenNullableDateTimeOffsetProperty_WhenSetScalarFromString_ThenConverts()
    {
        var target = new NullableTarget();
        _setter.SetScalar(target, "DateTimeOffsetProp", "2026-07-08T00:00:00+00:00", StringComparison.Ordinal);
        Assert.Equal(new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero), target.DateTimeOffsetProp);
    }

    // --- Pass-through ---

    [Fact]
    public void GivenValueAlreadyCorrectType_WhenSetScalar_ThenSetsWithoutConversion()
    {
        var target = new AllTypesTarget();
        _setter.SetScalar(target, "Int32Prop", 42, StringComparison.Ordinal);
        Assert.Equal(42, target.Int32Prop);
    }

    [Fact]
    public void GivenGuidAlreadyTyped_WhenSetScalar_ThenPassesThrough()
    {
        var target = new AllTypesTarget();
        var guid = Guid.NewGuid();
        _setter.SetScalar(target, "GuidProp", guid, StringComparison.Ordinal);
        Assert.Equal(guid, target.GuidProp);
    }

    // --- Invalid conversion ---

    [Fact]
    public void GivenInvalidStringForInt_WhenSetScalar_ThenThrowsTypeConversionException()
    {
        var target = new AllTypesTarget();
        Assert.Throws<TypeConversionException>(() =>
            _setter.SetScalar(target, "Int32Prop", "abc", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenInvalidStringForGuid_WhenSetScalar_ThenThrowsTypeConversionException()
    {
        var target = new AllTypesTarget();
        Assert.Throws<TypeConversionException>(() =>
            _setter.SetScalar(target, "GuidProp", "not-a-guid", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenInvalidStringForNullableInt_WhenSetScalar_ThenThrowsTypeConversionException()
    {
        var target = new NullableTarget();
        Assert.Throws<TypeConversionException>(() =>
            _setter.SetScalar(target, "IntProp", "abc", StringComparison.Ordinal));
    }

    // --- Test model classes ---

    public enum Color { Red, Green, Blue }

    private sealed class AllTypesTarget
    {
        public string? StringProp { get; set; }
        public bool BoolProp { get; set; }
        public char CharProp { get; set; }
        public byte ByteProp { get; set; }
        public sbyte SByteProp { get; set; }
        public short Int16Prop { get; set; }
        public ushort UInt16Prop { get; set; }
        public int Int32Prop { get; set; }
        public uint UInt32Prop { get; set; }
        public long Int64Prop { get; set; }
        public ulong UInt64Prop { get; set; }
        public float FloatProp { get; set; }
        public double DoubleProp { get; set; }
        public decimal DecimalProp { get; set; }
        public DateTime DateTimeProp { get; set; }
        public Color ColorProp { get; set; }
        public Guid GuidProp { get; set; }
        public TimeSpan TimeSpanProp { get; set; }
        public DateTimeOffset DateTimeOffsetProp { get; set; }
        public DateOnly DateOnlyProp { get; set; }
        public TimeOnly TimeOnlyProp { get; set; }
    }

    private sealed class NullableTarget
    {
        public int? IntProp { get; set; }
        public bool? BoolProp { get; set; }
        public DateTime? DateTimeProp { get; set; }
        public Guid? GuidProp { get; set; }
        public Color? ColorProp { get; set; }
        public decimal? DecimalProp { get; set; }
        public DateOnly? DateOnlyProp { get; set; }
        public TimeOnly? TimeOnlyProp { get; set; }
        public TimeSpan? TimeSpanProp { get; set; }
        public DateTimeOffset? DateTimeOffsetProp { get; set; }
    }
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PropertyPathSetterScalarTypesTests" -v minimal 2>&1 | tail -5`
Expected: Most tests fail with `NotImplementedException` from `ConvertValue`.

- [ ] **Step 3: Implement ConvertValue with the full conversion chain**

Replace the `ConvertValue` method in `src/Tokenizer/Reflection/PropertyPathSetter.cs`:

```csharp
    private static object ConvertValue(object value, Type targetType)
    {
        // 1. Pass-through: value is already the correct type
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // 2. Enum
        if (targetType.IsEnum)
        {
            return ConvertToEnum(value, targetType);
        }

        // 3. Nullable<T> — unwrap and recurse
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = targetType.GetGenericArguments()[0];
            return ConvertValue(value, underlyingType);
        }

        // 4. Non-IConvertible structs
        var converted = TryConvertNonIConvertible(value, targetType);
        if (converted != null)
        {
            return converted;
        }

        // 5. IConvertible primitives
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new TypeConversionException(
                $"Unable to convert '{value}' to type {targetType.Name}",
                value, targetType, ex);
        }
    }

    private static object ConvertToEnum(object value, Type enumType)
    {
        if (value.GetType() == enumType)
        {
            return value;
        }

        var valueString = value.ToString()
            ?? throw new TypeConversionException(
                $"Cannot convert null string to enum type {enumType.Name}",
                value, enumType);

        try
        {
            return Enum.Parse(enumType, valueString, ignoreCase: true);
        }
        catch (ArgumentException ex)
        {
            throw new TypeConversionException(
                $"Unable to convert '{valueString}' to enum type {enumType.Name}",
                value, enumType, ex);
        }
    }

    private static object? TryConvertNonIConvertible(object value, Type targetType)
    {
        var valueString = value.ToString();
        if (valueString == null) return null;

        try
        {
            if (targetType == typeof(Guid))
                return Guid.Parse(valueString);

            if (targetType == typeof(TimeSpan))
                return TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);

            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(valueString, CultureInfo.InvariantCulture);

#if NET6_0_OR_GREATER
            if (targetType == typeof(DateOnly))
                return DateOnly.Parse(valueString, CultureInfo.InvariantCulture);

            if (targetType == typeof(TimeOnly))
                return TimeOnly.Parse(valueString, CultureInfo.InvariantCulture);
#endif

            return null;
        }
        catch (FormatException ex)
        {
            throw new TypeConversionException(
                $"Unable to convert '{value}' to type {targetType.Name}",
                value, targetType, ex);
        }
    }
```

- [ ] **Step 4: Run all PropertyPathSetter tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~Tokens.Reflection.PropertyPathSetter" -v minimal`
Expected: All Tier 1 and Tier 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Reflection/PropertyPathSetter.cs tests/Tokenizer.Tests/Reflection/PropertyPathSetter.ScalarTypes.Tests.cs
git commit -m "feat: add exhaustive scalar type conversion to PropertyPathSetter"
```

---

### Task 4: Implement AssignCollection with all collection types and Tier 3 tests

**Files:**
- Modify: `src/Tokenizer/Reflection/PropertyPathSetter.cs`
- Create: `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Collections.Tests.cs`

This task replaces the `AssignCollection` stub with the full collection support: arrays, HashSet, ImmutableList, ImmutableArray, List/IList/ICollection, getter-only collections, and unsupported collection types.

- [ ] **Step 1: Write the Tier 3 test file**

Create `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Collections.Tests.cs`:

```csharp
using System.Collections.Immutable;
using Tokens.Reflection;
using Xunit;

namespace Tokens.Reflection;

public class PropertyPathSetterCollectionsTests
{
    private readonly PropertyPathSetter _setter = new();

    // --- List<T> ---

    [Fact]
    public void GivenListProperty_WhenSetCollection_ThenAssignsAllValues()
    {
        // Arrange
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "c" };

        // Act
        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        // Assert
        Assert.Equal(3, target.StringList!.Count);
        Assert.Equal(new[] { "a", "b", "c" }, target.StringList);
    }

    // --- IList<T> ---

    [Fact]
    public void GivenIListProperty_WhenSetCollection_ThenAssignsAllValues()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "x", "y" };

        _setter.SetCollection(target, "StringIList", values, StringComparison.Ordinal);

        Assert.Equal(2, target.StringIList!.Count);
        Assert.Equal("x", target.StringIList[0]);
        Assert.Equal("y", target.StringIList[1]);
    }

    // --- ICollection<T> ---

    [Fact]
    public void GivenICollectionProperty_WhenSetCollection_ThenAssignsAllValues()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "p", "q" };

        _setter.SetCollection(target, "StringICollection", values, StringComparison.Ordinal);

        Assert.Equal(2, target.StringICollection!.Count);
        Assert.Contains("p", target.StringICollection);
        Assert.Contains("q", target.StringICollection);
    }

    // --- T[] ---

    [Fact]
    public void GivenArrayProperty_WhenSetCollection_ThenAssignsArray()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "c" };

        _setter.SetCollection(target, "StringArray", values, StringComparison.Ordinal);

        Assert.NotNull(target.StringArray);
        Assert.Equal(new[] { "a", "b", "c" }, target.StringArray);
    }

    // --- HashSet<T> ---

    [Fact]
    public void GivenHashSetProperty_WhenSetCollectionWithUniqueValues_ThenAssigns()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "c" };

        _setter.SetCollection(target, "StringHashSet", values, StringComparison.Ordinal);

        Assert.NotNull(target.StringHashSet);
        Assert.Equal(3, target.StringHashSet!.Count);
        Assert.Contains("a", target.StringHashSet);
        Assert.Contains("b", target.StringHashSet);
        Assert.Contains("c", target.StringHashSet);
    }

    [Fact]
    public void GivenHashSetProperty_WhenSetCollectionWithDuplicates_ThenThrowsWithDuplicateValue()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "a", "b", "a" };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetCollection(target, "StringHashSet", values, StringComparison.Ordinal));
        Assert.Contains("a", ex.Message);
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- ImmutableList<T> ---

    [Fact]
    public void GivenImmutableListProperty_WhenSetCollection_ThenAssigns()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "x", "y", "z" };

        _setter.SetCollection(target, "StringImmutableList", values, StringComparison.Ordinal);

        Assert.NotNull(target.StringImmutableList);
        Assert.Equal(3, target.StringImmutableList!.Count);
        Assert.Equal(new[] { "x", "y", "z" }, target.StringImmutableList);
    }

    // --- ImmutableArray<T> ---

    [Fact]
    public void GivenImmutableArrayProperty_WhenSetCollection_ThenAssigns()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "1", "2" };

        _setter.SetCollection(target, "StringImmutableArray", values, StringComparison.Ordinal);

        Assert.Equal(2, target.StringImmutableArray.Length);
        Assert.Equal("1", target.StringImmutableArray[0]);
        Assert.Equal("2", target.StringImmutableArray[1]);
    }

    // --- Element type conversion ---

    [Fact]
    public void GivenListOfInt_WhenSetCollectionFromStrings_ThenConvertsElements()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "1", "2", "3" };

        _setter.SetCollection(target, "IntList", values, StringComparison.Ordinal);

        Assert.Equal(new[] { 1, 2, 3 }, target.IntList);
    }

    [Fact]
    public void GivenIntArray_WhenSetCollectionFromStrings_ThenConvertsElements()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "10", "20" };

        _setter.SetCollection(target, "IntArray", values, StringComparison.Ordinal);

        Assert.Equal(new[] { 10, 20 }, target.IntArray);
    }

    // --- Empty values ---

    [Fact]
    public void GivenListProperty_WhenSetCollectionWithEmptyValues_ThenAssignsEmptyCollection()
    {
        var target = new CollectionTarget();
        var values = new List<object>();

        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        Assert.NotNull(target.StringList);
        Assert.Empty(target.StringList);
    }

    [Fact]
    public void GivenArrayProperty_WhenSetCollectionWithEmptyValues_ThenAssignsEmptyArray()
    {
        var target = new CollectionTarget();
        var values = new List<object>();

        _setter.SetCollection(target, "StringArray", values, StringComparison.Ordinal);

        Assert.NotNull(target.StringArray);
        Assert.Empty(target.StringArray);
    }

    // --- Getter-only collection with existing instance ---

    [Fact]
    public void GivenGetterOnlyListWithExistingInstance_WhenSetCollection_ThenAddsToExisting()
    {
        var target = new GetterOnlyCollectionTarget();
        var values = new List<object> { "first", "second" };

        _setter.SetCollection(target, "Tags", values, StringComparison.Ordinal);

        Assert.Equal(2, target.Tags.Count);
        Assert.Equal("first", target.Tags[0]);
        Assert.Equal("second", target.Tags[1]);
    }

    // --- Getter-only array/immutable (unsupported) ---

    [Fact]
    public void GivenGetterOnlyArrayProperty_WhenSetCollection_ThenThrowsInvalidOperationException()
    {
        var target = new GetterOnlyArrayTarget();
        var values = new List<object> { "a" };

        Assert.Throws<InvalidOperationException>(() =>
            _setter.SetCollection(target, "Items", values, StringComparison.Ordinal));
    }

    [Fact]
    public void GivenGetterOnlyImmutableListProperty_WhenSetCollection_ThenThrowsInvalidOperationException()
    {
        var target = new GetterOnlyImmutableTarget();
        var values = new List<object> { "a" };

        Assert.Throws<InvalidOperationException>(() =>
            _setter.SetCollection(target, "Items", values, StringComparison.Ordinal));
    }

    // --- Single value to collection ---

    [Fact]
    public void GivenListProperty_WhenSetCollectionWithSingleValue_ThenCreatesSingleElementCollection()
    {
        var target = new CollectionTarget();
        var values = new List<object> { "only" };

        _setter.SetCollection(target, "StringList", values, StringComparison.Ordinal);

        Assert.Single(target.StringList!);
        Assert.Equal("only", target.StringList![0]);
    }

    // --- Unsupported collection types ---

    [Fact]
    public void GivenDictionaryProperty_WhenSetCollection_ThenThrowsInvalidOperationException()
    {
        var target = new UnsupportedCollectionTarget();
        var values = new List<object> { "a" };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetCollection(target, "Dict", values, StringComparison.Ordinal));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivenBareIEnumerableProperty_WhenSetCollection_ThenThrowsInvalidOperationException()
    {
        var target = new UnsupportedCollectionTarget();
        var values = new List<object> { "a" };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _setter.SetCollection(target, "Enumerable", values, StringComparison.Ordinal));
        Assert.Contains("not supported", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- IsCollectionProperty ---

    [Fact]
    public void GivenListProperty_WhenIsCollectionProperty_ThenReturnsTrue()
    {
        Assert.True(_setter.IsCollectionProperty(typeof(CollectionTarget), "StringList", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenArrayProperty_WhenIsCollectionProperty_ThenReturnsTrue()
    {
        Assert.True(_setter.IsCollectionProperty(typeof(CollectionTarget), "StringArray", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenHashSetProperty_WhenIsCollectionProperty_ThenReturnsTrue()
    {
        Assert.True(_setter.IsCollectionProperty(typeof(CollectionTarget), "StringHashSet", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenImmutableListProperty_WhenIsCollectionProperty_ThenReturnsTrue()
    {
        Assert.True(_setter.IsCollectionProperty(typeof(CollectionTarget), "StringImmutableList", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenScalarProperty_WhenIsCollectionProperty_ThenReturnsFalse()
    {
        Assert.False(_setter.IsCollectionProperty(typeof(CollectionTarget), "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDictionaryProperty_WhenIsCollectionProperty_ThenReturnsFalse()
    {
        Assert.False(_setter.IsCollectionProperty(typeof(UnsupportedCollectionTarget), "Dict", StringComparison.Ordinal));
    }

    // --- Test model classes ---

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
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PropertyPathSetterCollectionsTests" -v minimal 2>&1 | tail -5`
Expected: Most tests fail with `NotImplementedException` from `AssignCollection`.

- [ ] **Step 3: Implement AssignCollection**

Replace the `AssignCollection` method in `src/Tokenizer/Reflection/PropertyPathSetter.cs`:

```csharp
    private void AssignCollection(object target, PropertyInfo property, IReadOnlyList<object> values)
    {
        var propertyType = property.PropertyType;
        var elementType = GetCollectionElementType(propertyType);

        if (elementType == null)
        {
            throw new InvalidOperationException(
                $"Collection type '{propertyType.Name}' is not supported for property '{property.Name}'. " +
                $"Use List<T>, T[], HashSet<T>, ImmutableList<T>, or ImmutableArray<T> instead.");
        }

        var convertedValues = ConvertElements(values, elementType);

        // Array
        if (propertyType.IsArray)
        {
            AssignArray(target, property, convertedValues, elementType);
            return;
        }

        if (!propertyType.IsGenericType)
        {
            throw new InvalidOperationException(
                $"Collection type '{propertyType.Name}' is not supported for property '{property.Name}'.");
        }

        var genericDef = propertyType.GetGenericTypeDefinition();

        // HashSet<T>
        if (genericDef == typeof(HashSet<>))
        {
            AssignHashSet(target, property, convertedValues, elementType);
            return;
        }

        // Immutable collections (detected by type name to avoid hard dependency)
        var fullName = genericDef.FullName;
        if (fullName == "System.Collections.Immutable.ImmutableList`1" ||
            fullName == "System.Collections.Immutable.ImmutableArray`1")
        {
            AssignImmutableCollection(target, property, convertedValues, propertyType);
            return;
        }

        // List<T> / IList<T> / ICollection<T>
        if (genericDef == typeof(List<>) ||
            genericDef == typeof(IList<>) ||
            genericDef == typeof(ICollection<>))
        {
            AssignList(target, property, convertedValues, elementType);
            return;
        }

        throw new InvalidOperationException(
            $"Collection type '{propertyType.Name}' is not supported for property '{property.Name}'. " +
            $"Use List<T>, T[], HashSet<T>, ImmutableList<T>, or ImmutableArray<T> instead.");
    }

    private static Type? GetCollectionElementType(Type propertyType)
    {
        if (propertyType.IsArray)
        {
            return propertyType.GetElementType();
        }

        if (!propertyType.IsGenericType) return null;

        var genericDef = propertyType.GetGenericTypeDefinition();

        if (genericDef == typeof(List<>) ||
            genericDef == typeof(IList<>) ||
            genericDef == typeof(ICollection<>) ||
            genericDef == typeof(HashSet<>))
        {
            return propertyType.GetGenericArguments()[0];
        }

        var fullName = genericDef.FullName;
        if (fullName == "System.Collections.Immutable.ImmutableList`1" ||
            fullName == "System.Collections.Immutable.ImmutableArray`1")
        {
            return propertyType.GetGenericArguments()[0];
        }

        return null;
    }

    private List<object> ConvertElements(IReadOnlyList<object> values, Type elementType)
    {
        var converted = new List<object>(values.Count);
        foreach (var value in values)
        {
            converted.Add(ConvertValue(value, elementType));
        }
        return converted;
    }

    private static void AssignArray(object target, PropertyInfo property, List<object> values, Type elementType)
    {
        ThrowIfReadOnly(target, property);
        var array = Array.CreateInstance(elementType, values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            array.SetValue(values[i], i);
        }
        property.SetValue(target, array);
    }

    private static void AssignHashSet(object target, PropertyInfo property, List<object> values, Type elementType)
    {
        ThrowIfReadOnly(target, property);
        var setType = typeof(HashSet<>).MakeGenericType(elementType);
        var set = Activator.CreateInstance(setType)!;
        var addMethod = setType.GetMethod("Add")!;

        foreach (var value in values)
        {
            var added = (bool)addMethod.Invoke(set, new[] { value })!;
            if (!added)
            {
                throw new InvalidOperationException(
                    $"Duplicate value '{value}' for HashSet property '{property.Name}'.");
            }
        }

        property.SetValue(target, set);
    }

    private static void AssignImmutableCollection(
        object target, PropertyInfo property, List<object> values, Type propertyType)
    {
        ThrowIfReadOnly(target, property);

        var elementType = propertyType.GetGenericArguments()[0];
        var typedListType = typeof(List<>).MakeGenericType(elementType);
        var typedList = Activator.CreateInstance(typedListType)!;
        var addMethod = typedListType.GetMethod("Add")!;
        foreach (var value in values)
        {
            addMethod.Invoke(typedList, new[] { value });
        }

        // Call the static CreateRange method on the immutable type
        var immutableTypeName = propertyType.GetGenericTypeDefinition().FullName!;
        var nonGenericTypeName = immutableTypeName.Replace("`1", "");
        var immutableStaticType = propertyType.Assembly.GetType(nonGenericTypeName);

        if (immutableStaticType == null)
        {
            throw new InvalidOperationException(
                $"Collection type '{propertyType.Name}' is not supported for property '{property.Name}'.");
        }

        var createRangeMethod = immutableStaticType
            .GetMethods()
            .FirstOrDefault(m => m.Name == "CreateRange" &&
                                 m.GetParameters().Length == 1 &&
                                 m.IsGenericMethodDefinition);

        if (createRangeMethod == null)
        {
            throw new InvalidOperationException(
                $"Collection type '{propertyType.Name}' is not supported for property '{property.Name}'.");
        }

        var genericCreateRange = createRangeMethod.MakeGenericMethod(elementType);
        var result = genericCreateRange.Invoke(null, new[] { typedList });
        property.SetValue(target, result);
    }

    private static void AssignList(object target, PropertyInfo property, List<object> values, Type elementType)
    {
        // Check for getter-only property with existing collection instance
        if (!property.CanWrite || property.GetSetMethod() == null)
        {
            var existing = property.GetValue(target);
            if (existing == null)
            {
                throw new InvalidOperationException(
                    $"Cannot set property '{property.Name}' on type '{target.GetType().Name}': " +
                    $"property is read-only and the collection is null.");
            }

            // Add to existing collection via IList interface
            var list = existing as System.Collections.IList
                ?? throw new InvalidOperationException(
                    $"Cannot add to property '{property.Name}': existing collection does not support IList.");

            foreach (var value in values)
            {
                list.Add(value);
            }
            return;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var newList = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var value in values)
        {
            newList.Add(value);
        }
        property.SetValue(target, newList);
    }

    private static void ThrowIfReadOnly(object target, PropertyInfo property)
    {
        if (!property.CanWrite || property.GetSetMethod() == null)
        {
            throw new InvalidOperationException(
                $"Cannot set property '{property.Name}' on type '{target.GetType().Name}': " +
                $"property is read-only.");
        }
    }
```

- [ ] **Step 4: Run all PropertyPathSetter tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~Tokens.Reflection.PropertyPathSetter" -v minimal`
Expected: All Tier 1, 2, and 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Tokenizer/Reflection/PropertyPathSetter.cs tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Collections.Tests.cs
git commit -m "feat: add collection assignment to PropertyPathSetter"
```

---

### Task 5: Rewire Assign\<T\>() and delete ObjectExtensions

**Files:**
- Modify: `src/Tokenizer/TokenizeResult.cs`
- Delete: `src/Tokenizer/Extensions/ObjectExtensions.cs`
- Delete: `tests/Tokenizer.Tests/Extensions/ObjectExtensionsTests.cs`
- Delete: `tests/Tokenizer.Tests/Extensions/ObjectExtensionsPathTests.cs`
- Delete: `tests/Tokenizer.Tests/Extensions/ObjectExtensionsPropertyCacheTests.cs`

This task rewrites `Assign<T>()` to use `PropertyPathSetter` with match grouping, then deletes `ObjectExtensions` and its tests.

- [ ] **Step 1: Rewrite Assign\<T\>() in TokenizeResult.cs**

Replace the contents of `src/Tokenizer/TokenizeResult.cs`:

```csharp
using Tokens.Exceptions;
using Tokens.Reflection;

namespace Tokens;

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public sealed class TokenizeResult
{
    private readonly List<Exception> _exceptions;

    /// <summary>
    /// Creates a new result bound to the specified <paramref name="template"/>.
    /// </summary>
    public TokenizeResult(Template template)
    {
        _exceptions = new List<Exception>();
        Hints = new HintResult();
        Tokens = new TokenResult();
        Template = template;
    }

    /// <summary>
    /// The template used for the tokenization attempt.
    /// </summary>
    public Template Template { get; init; }

    /// <summary>
    /// A list of any exceptions that occurred during the matching process.
    /// </summary>
    public IReadOnlyList<Exception> Exceptions => _exceptions;

    /// <summary>
    /// The matches that were made during the tokenization process.
    /// </summary>
    public TokenResult Tokens { get; init; }

    /// <summary>
    /// Gets the hints found in the input.
    /// </summary>
    public HintResult Hints { get; init; }

    internal void AddException(Exception exception)
    {
        _exceptions.Add(exception);
    }

    /// <summary>
    /// Structured diagnostic output from the tokenization process.
    /// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
    /// </summary>
    public Diagnostics.DiagnosticResult? Diagnostics { get; internal set; }

    /// <summary>
    /// Determines whether the matching process was successful.
    /// </summary>
    public bool Success => Tokens.HasMatches &&
                           !Tokens.HasMissingRequiredTokens &&
                           !Hints.HasMissingRequiredHints &&
                           (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));

    /// <summary>
    /// A read-only list of values extracted from the input string.
    /// </summary>
    public IReadOnlyList<TokenMatch> Matches => Tokens.Matches;

    /// <inheritdoc />
    public override string ToString() =>
        $"TokenizeResult('{Template.Name}': {Tokens.Matches.Count} matched, {Tokens.Misses.Count} missed)";

    /// <summary>
    /// Projects matches onto a new instance of <typeparamref name="T"/>,
    /// assigning matched values to the object's properties via reflection.
    /// </summary>
    /// <typeparam name="T">The type to populate with matched values.</typeparam>
    /// <returns>A new instance of <typeparamref name="T"/> with populated properties.</returns>
    /// <exception cref="AssignmentFailedException">
    /// Thrown when one or more matched values cannot be assigned to the target's properties.
    /// </exception>
    public T Assign<T>() where T : class, new()
    {
        var target = new T();
        var options = Template.Options;
        var errors = new List<Exception>();
        var setter = new PropertyPathSetter();

        var groups = Matches
            .GroupBy(m => m.Token.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var group in groups)
        {
            try
            {
                var path = group.Key;
                var values = group.Select(m => m.Value).ToList();

                if (setter.IsCollectionProperty(typeof(T), path, StringComparison.Ordinal))
                {
                    setter.SetCollection(target, path, values, StringComparison.Ordinal);
                }
                else
                {
                    // For scalar properties, use the last matched value
                    setter.SetScalar(target, path, values[values.Count - 1], StringComparison.Ordinal);
                }
            }
            catch (MissingMemberException)
            {
                if (!options.IgnoreMissingProperties)
                {
                    errors.Add(new MissingMemberException(
                        $"Property '{group.Key}' not found on type '{typeof(T).Name}'."));
                }
            }
            catch (TypeConversionException ex)
            {
                errors.Add(ex);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AssignmentFailedException(
                $"Failed to assign {errors.Count} value(s) to type '{typeof(T).Name}'.", errors);
        }

        return target;
    }
}
```

- [ ] **Step 2: Delete ObjectExtensions.cs**

```bash
git rm src/Tokenizer/Extensions/ObjectExtensions.cs
```

- [ ] **Step 3: Fix any remaining references to ObjectExtensions**

Check if any production code still references the deleted class. The `using Tokens.Extensions;` imports in other files that only use `StringExtensions` will be cleaned up by the IDE0005 analyzer. Run a build to find errors:

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release 2>&1 | grep -i error`
Expected: No compile errors. If `IDE0005` fires on unused `using Tokens.Extensions;` in files that only had it for `ObjectExtensions`, run `dotnet format style ./Tokenizer.sln --diagnostics IDE0005` to auto-fix.

- [ ] **Step 4: Delete the old ObjectExtensions test files**

```bash
git rm tests/Tokenizer.Tests/Extensions/ObjectExtensionsTests.cs
git rm tests/Tokenizer.Tests/Extensions/ObjectExtensionsPathTests.cs
git rm tests/Tokenizer.Tests/Extensions/ObjectExtensionsPropertyCacheTests.cs
```

- [ ] **Step 5: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v minimal`
Expected: All tests pass. Existing tests that use `Assign<T>()` (e.g., `TokenizeResultAssignTests`, `TemplateMatcherTests`, `ConcatenationTests`) should still work because `Assign<T>()` is the public API — only its internals changed.

- [ ] **Step 6: Commit**

```bash
git add -A
git status
git commit -m "refactor: rewire Assign<T>() to PropertyPathSetter, delete ObjectExtensions"
```

---

### Task 6: Add Tier 4 pipeline integration tests

**Files:**
- Create: `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Pipeline.Tests.cs`

This task adds end-to-end tests that verify the full pipeline: template → tokenize → `Assign<T>()` → populated object. These tests confirm that `PropertyPathSetter` works correctly through the tokenizer's public API.

- [ ] **Step 1: Write the Tier 4 test file**

Create `tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Pipeline.Tests.cs`:

```csharp
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Reflection;

public class PropertyPathSetterPipelineTests : TokenizerTestBase
{
    public PropertyPathSetterPipelineTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenStringToken_WhenAssign_ThenSetsStringProperty()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenIntToken_WhenAssign_ThenSetsIntProperty()
    {
        var token = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, 42, new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal(42, target.Age);
    }

    [Fact]
    public void GivenStringValueForIntProperty_WhenAssign_ThenConvertsAndSets()
    {
        var token = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "25", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal(25, target.Age);
    }

    [Fact]
    public void GivenNullableProperty_WhenAssign_ThenSetsNullableValue()
    {
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "99", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal(99, target.Score);
    }

    [Fact]
    public void GivenBoolProperty_WhenAssign_ThenConvertsBool()
    {
        var token = new TokenBuilder().WithName("IsActive").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "true", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.True(target.IsActive);
    }

    [Fact]
    public void GivenDecimalProperty_WhenAssign_ThenConvertsDecimal()
    {
        var token = new TokenBuilder().WithName("Price").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "19.99", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal(19.99m, target.Price);
    }

    [Fact]
    public void GivenGuidProperty_WhenAssign_ThenConvertsGuid()
    {
        var guid = Guid.NewGuid();
        var token = new TokenBuilder().WithName("Id").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, guid.ToString(), new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal(guid, target.Id);
    }

    [Fact]
    public void GivenMultipleTokensSameName_WhenAssignToListProperty_ThenGroupsIntoCollection()
    {
        var token = new TokenBuilder().WithName("Tags").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(token, "tag1", new FileLocation()),
                new TokenMatch(token, "tag2", new FileLocation()),
                new TokenMatch(token, "tag3", new FileLocation()))
            .Build();

        var target = result.Assign<CollectionTarget>();

        Assert.Equal(3, target.Tags!.Count);
        Assert.Equal(new[] { "tag1", "tag2", "tag3" }, target.Tags);
    }

    [Fact]
    public void GivenMultipleTokensSameName_WhenAssignToArrayProperty_ThenGroupsIntoArray()
    {
        var token = new TokenBuilder().WithName("Items").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(token, "a", new FileLocation()),
                new TokenMatch(token, "b", new FileLocation()))
            .Build();

        var target = result.Assign<CollectionTarget>();

        Assert.NotNull(target.Items);
        Assert.Equal(new[] { "a", "b" }, target.Items);
    }

    [Fact]
    public void GivenMixedScalarAndCollectionTokens_WhenAssign_ThenAssignsBoth()
    {
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var tagToken = new TokenBuilder().WithName("Tags").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, tagToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(tagToken, "dev", new FileLocation()),
                new TokenMatch(tagToken, "ops", new FileLocation()))
            .Build();

        var target = result.Assign<CollectionTarget>();

        Assert.Equal("Alice", target.Name);
        Assert.Equal(new[] { "dev", "ops" }, target.Tags);
    }

    [Fact]
    public void GivenIgnoreMissingPropertiesTrue_WhenAssign_ThenSkipsMissingProperties()
    {
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var unknownToken = new TokenBuilder().WithName("Unknown").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, unknownToken).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(unknownToken, "ignored", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenIgnoreMissingPropertiesFalse_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        var token = new TokenBuilder().WithName("Unknown").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<ScalarTarget>());
        Assert.Single(ex.Errors);
        Assert.IsType<MissingMemberException>(ex.Errors[0]);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        var token = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<ScalarTarget>());
        Assert.Single(ex.Errors);
    }

    [Fact]
    public void GivenNestedProperty_WhenAssign_ThenTraversesAndSets()
    {
        var token = new TokenBuilder().WithName("Address.City").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "London", new FileLocation()))
            .Build();

        var target = result.Assign<ScalarTarget>();

        Assert.NotNull(target.Address);
        Assert.Equal("London", target.Address!.City);
    }

    // --- Test model classes ---

    public sealed class ScalarTarget
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public int? Score { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public Guid Id { get; set; }
        public AddressTarget? Address { get; set; }
    }

    public sealed class AddressTarget
    {
        public string? City { get; set; }
    }

    public sealed class CollectionTarget
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public string[]? Items { get; set; }
    }
}
```

- [ ] **Step 2: Run the Tier 4 tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "PropertyPathSetterPipelineTests" -v minimal`
Expected: All tests pass.

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v minimal`
Expected: All tests pass — both the new tests and all existing tests.

- [ ] **Step 4: Commit**

```bash
git add tests/Tokenizer.Tests/Reflection/PropertyPathSetter.Pipeline.Tests.cs
git commit -m "test: add Tier 4 pipeline integration tests for PropertyPathSetter"
```

---

### Task 7: Final verification and cleanup

**Files:**
- All files from previous tasks

- [ ] **Step 1: Verify ObjectExtensions directory is clean**

Run: `ls src/Tokenizer/Extensions/`
Expected: `ObjectExtensions.cs` should not be listed. `StringExtensions.cs` and any other extension files should still be present.

Run: `ls tests/Tokenizer.Tests/Extensions/`
Expected: No `ObjectExtensions*` test files.

- [ ] **Step 2: Verify no remaining references to ObjectExtensions**

Run: `grep -r "ObjectExtensions\|SetInnerValue\|GetInnerValue\|AddMethodCache" src/ tests/ --include="*.cs" | grep -v ".git"`
Expected: No matches.

- [ ] **Step 3: Run the full test suite one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj -v minimal`
Expected: All tests pass.

- [ ] **Step 4: Verify the build succeeds with analyzers**

Run: `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release 2>&1 | grep -E "(error|warning)" | head -20`
Expected: No errors, no new warnings.

- [ ] **Step 5: Commit if there are any cleanup changes**

```bash
git status
# If there are changes:
git add -A
git commit -m "chore: final cleanup after PropertyPathSetter migration"
```
