using Tokens.Exceptions;
using Xunit;

namespace Tokens.Reflection;

public class PropertyPathSetterScalarTypesTests
{
    // ── Shared state ────────────────────────────────────────────────────────────

    private readonly PropertyPathSetter _setter = new();

    // ── Model classes ───────────────────────────────────────────────────────────

    public enum Color { Red, Green, Blue }

    private sealed class AllTypesTarget
    {
        public string StringProp { get; set; } = null!;
        public bool BoolProp { get; set; }
        public char CharProp { get; set; }
        public byte ByteProp { get; set; }
        public sbyte SByteProp { get; set; }
        public short ShortProp { get; set; }
        public ushort UShortProp { get; set; }
        public int IntProp { get; set; }
        public uint UIntProp { get; set; }
        public long LongProp { get; set; }
        public ulong ULongProp { get; set; }
        public float FloatProp { get; set; }
        public double DoubleProp { get; set; }
        public decimal DecimalProp { get; set; }
        public DateTime DateTimeProp { get; set; }
        public Color ColorProp { get; set; }
        public Guid GuidProp { get; set; }
        public TimeSpan TimeSpanProp { get; set; }
        public DateTimeOffset DateTimeOffsetProp { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly DateOnlyProp { get; set; }
        public TimeOnly TimeOnlyProp { get; set; }
#endif
    }

    private sealed class NullableTarget
    {
        public int? NullableInt { get; set; }
        public bool? NullableBool { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public Guid? NullableGuid { get; set; }
        public Color? NullableColor { get; set; }
        public decimal? NullableDecimal { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly? NullableDateOnly { get; set; }
        public TimeOnly? NullableTimeOnly { get; set; }
#endif
        public TimeSpan? NullableTimeSpan { get; set; }
        public DateTimeOffset? NullableDateTimeOffset { get; set; }
    }

    // ── IConvertible primitives ─────────────────────────────────────────────────

    [Fact]
    public void GivenStringValue_WhenSetScalarToStringProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "StringProp", "hello", StringComparison.Ordinal);

        // Assert
        Assert.Equal("hello", target.StringProp);
    }

    [Fact]
    public void GivenStringTrue_WhenSetScalarToBoolProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "BoolProp", "True", StringComparison.Ordinal);

        // Assert
        Assert.True(target.BoolProp);
    }

    [Fact]
    public void GivenStringChar_WhenSetScalarToCharProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "CharProp", "A", StringComparison.Ordinal);

        // Assert
        Assert.Equal('A', target.CharProp);
    }

    [Fact]
    public void GivenStringByte_WhenSetScalarToByteProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ByteProp", "255", StringComparison.Ordinal);

        // Assert
        Assert.Equal(255, target.ByteProp);
    }

    [Fact]
    public void GivenStringSByte_WhenSetScalarToSByteProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "SByteProp", "-1", StringComparison.Ordinal);

        // Assert
        Assert.Equal(-1, target.SByteProp);
    }

    [Fact]
    public void GivenStringShort_WhenSetScalarToShortProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ShortProp", "32767", StringComparison.Ordinal);

        // Assert
        Assert.Equal(32767, target.ShortProp);
    }

    [Fact]
    public void GivenStringUShort_WhenSetScalarToUShortProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "UShortProp", "65535", StringComparison.Ordinal);

        // Assert
        Assert.Equal(65535, target.UShortProp);
    }

    [Fact]
    public void GivenStringInt_WhenSetScalarToIntProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "IntProp", "42", StringComparison.Ordinal);

        // Assert
        Assert.Equal(42, target.IntProp);
    }

    [Fact]
    public void GivenStringUInt_WhenSetScalarToUIntProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "UIntProp", "4294967295", StringComparison.Ordinal);

        // Assert
        Assert.Equal(4294967295u, target.UIntProp);
    }

    [Fact]
    public void GivenStringLong_WhenSetScalarToLongProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "LongProp", "9223372036854775807", StringComparison.Ordinal);

        // Assert
        Assert.Equal(long.MaxValue, target.LongProp);
    }

    [Fact]
    public void GivenStringULong_WhenSetScalarToULongProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ULongProp", "18446744073709551615", StringComparison.Ordinal);

        // Assert
        Assert.Equal(ulong.MaxValue, target.ULongProp);
    }

    [Fact]
    public void GivenStringFloat_WhenSetScalarToFloatProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "FloatProp", "3.14", StringComparison.Ordinal);

        // Assert
        Assert.Equal(3.14f, target.FloatProp, precision: 2);
    }

    [Fact]
    public void GivenStringDouble_WhenSetScalarToDoubleProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "DoubleProp", "3.14159", StringComparison.Ordinal);

        // Assert
        Assert.Equal(3.14159, target.DoubleProp, precision: 5);
    }

    [Fact]
    public void GivenStringDecimal_WhenSetScalarToDecimalProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "DecimalProp", "123.456", StringComparison.Ordinal);

        // Assert
        Assert.Equal(123.456m, target.DecimalProp);
    }

    [Fact]
    public void GivenStringDateTime_WhenSetScalarToDateTimeProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "DateTimeProp", "2026-07-08T00:00:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 8, 0, 0, 0), target.DateTimeProp);
    }

    // ── Enum ────────────────────────────────────────────────────────────────────

    [Fact]
    public void GivenEnumNameString_WhenSetScalarToEnumProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ColorProp", "Green", StringComparison.Ordinal);

        // Assert
        Assert.Equal(Color.Green, target.ColorProp);
    }

    [Fact]
    public void GivenLowercaseEnumName_WhenSetScalarToEnumProp_ThenAssignedCaseInsensitive()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ColorProp", "green", StringComparison.Ordinal);

        // Assert
        Assert.Equal(Color.Green, target.ColorProp);
    }

    [Fact]
    public void GivenInvalidEnumName_WhenSetScalarToEnumProp_ThenThrowsTypeConversionException()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act & Assert
        Assert.Throws<TypeConversionException>(
            () => _setter.SetScalar(target, "ColorProp", "Purple", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenAlreadyTypedEnum_WhenSetScalarToEnumProp_ThenPassThrough()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "ColorProp", Color.Blue, StringComparison.Ordinal);

        // Assert
        Assert.Equal(Color.Blue, target.ColorProp);
    }

    // ── Non-IConvertible structs ────────────────────────────────────────────────

    [Fact]
    public void GivenGuidString_WhenSetScalarToGuidProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();
        var guid = Guid.NewGuid();

        // Act
        _setter.SetScalar(target, "GuidProp", guid.ToString(), StringComparison.Ordinal);

        // Assert
        Assert.Equal(guid, target.GuidProp);
    }

    [Fact]
    public void GivenTimeSpanString_WhenSetScalarToTimeSpanProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "TimeSpanProp", "01:30:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new TimeSpan(1, 30, 0), target.TimeSpanProp);
    }

    [Fact]
    public void GivenDateTimeOffsetString_WhenSetScalarToDateTimeOffsetProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "DateTimeOffsetProp", "2026-07-08T12:00:00+02:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.FromHours(2)), target.DateTimeOffsetProp);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenDateOnlyString_WhenSetScalarToDateOnlyProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "DateOnlyProp", "2026-07-08", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 8), target.DateOnlyProp);
    }

    [Fact]
    public void GivenTimeOnlyString_WhenSetScalarToTimeOnlyProp_ThenAssigned()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "TimeOnlyProp", "14:30:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new TimeOnly(14, 30, 0), target.TimeOnlyProp);
    }
#endif

    // ── Nullable<T> ─────────────────────────────────────────────────────────────

    [Fact]
    public void GivenStringInt_WhenSetScalarToNullableIntProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableInt", "99", StringComparison.Ordinal);

        // Assert
        Assert.Equal(99, target.NullableInt);
    }

    [Fact]
    public void GivenStringBool_WhenSetScalarToNullableBoolProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableBool", "False", StringComparison.Ordinal);

        // Assert
        Assert.Equal(false, target.NullableBool);
    }

    [Fact]
    public void GivenStringDateTime_WhenSetScalarToNullableDateTimeProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableDateTime", "2026-01-01T00:00:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), target.NullableDateTime);
    }

    [Fact]
    public void GivenGuidString_WhenSetScalarToNullableGuidProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();
        var guid = Guid.NewGuid();

        // Act
        _setter.SetScalar(target, "NullableGuid", guid.ToString(), StringComparison.Ordinal);

        // Assert
        Assert.Equal(guid, target.NullableGuid);
    }

    [Fact]
    public void GivenEnumName_WhenSetScalarToNullableEnumProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableColor", "Red", StringComparison.Ordinal);

        // Assert
        Assert.Equal(Color.Red, target.NullableColor);
    }

    [Fact]
    public void GivenStringDecimal_WhenSetScalarToNullableDecimalProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableDecimal", "9.99", StringComparison.Ordinal);

        // Assert
        Assert.Equal(9.99m, target.NullableDecimal);
    }

    [Fact]
    public void GivenTimeSpanString_WhenSetScalarToNullableTimeSpanProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableTimeSpan", "02:00:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new TimeSpan(2, 0, 0), target.NullableTimeSpan);
    }

    [Fact]
    public void GivenDateTimeOffsetString_WhenSetScalarToNullableDateTimeOffsetProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableDateTimeOffset", "2026-07-08T12:00:00+02:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.FromHours(2)), target.NullableDateTimeOffset);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenDateOnlyString_WhenSetScalarToNullableDateOnlyProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableDateOnly", "2026-07-08", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new DateOnly(2026, 7, 8), target.NullableDateOnly);
    }

    [Fact]
    public void GivenTimeOnlyString_WhenSetScalarToNullableTimeOnlyProp_ThenAssigned()
    {
        // Arrange
        var target = new NullableTarget();

        // Act
        _setter.SetScalar(target, "NullableTimeOnly", "14:30:00", StringComparison.Ordinal);

        // Assert
        Assert.Equal(new TimeOnly(14, 30, 0), target.NullableTimeOnly);
    }
#endif

    // ── Pass-through ────────────────────────────────────────────────────────────

    [Fact]
    public void GivenIntValue_WhenSetScalarToIntProp_ThenPassThrough()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act
        _setter.SetScalar(target, "IntProp", 123, StringComparison.Ordinal);

        // Assert
        Assert.Equal(123, target.IntProp);
    }

    [Fact]
    public void GivenGuidValue_WhenSetScalarToGuidProp_ThenPassThrough()
    {
        // Arrange
        var target = new AllTypesTarget();
        var guid = Guid.NewGuid();

        // Act
        _setter.SetScalar(target, "GuidProp", guid, StringComparison.Ordinal);

        // Assert
        Assert.Equal(guid, target.GuidProp);
    }

    // ── Invalid conversions ─────────────────────────────────────────────────────

    [Fact]
    public void GivenNonNumericString_WhenSetScalarToIntProp_ThenThrowsTypeConversionException()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act & Assert
        Assert.Throws<TypeConversionException>(
            () => _setter.SetScalar(target, "IntProp", "abc", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenInvalidGuidString_WhenSetScalarToGuidProp_ThenThrowsTypeConversionException()
    {
        // Arrange
        var target = new AllTypesTarget();

        // Act & Assert
        Assert.Throws<TypeConversionException>(
            () => _setter.SetScalar(target, "GuidProp", "not-a-guid", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenNonNumericString_WhenSetScalarToNullableIntProp_ThenThrowsTypeConversionException()
    {
        // Arrange
        var target = new NullableTarget();

        // Act & Assert
        Assert.Throws<TypeConversionException>(
            () => _setter.SetScalar(target, "NullableInt", "abc", StringComparison.Ordinal));
    }
}
