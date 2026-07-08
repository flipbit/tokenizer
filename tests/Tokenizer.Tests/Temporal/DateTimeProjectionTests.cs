using Xunit;

namespace Tokens.Temporal;

public class DateTimeProjectionTests
{
    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToDateTimeOffset_ThenReturnsDirectly()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var result = DateTimeProjection.Project(source, typeof(DateTimeOffset));

        // Assert
        Assert.Equal(source, result);
    }

    [Fact]
    public void GivenUtcDateTimeOffset_WhenProjectingToDateTime_ThenReturnsUtcKind()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);

        // Act
        var dt = (DateTime)DateTimeProjection.Project(source, typeof(DateTime));

        // Assert
        Assert.Equal(DateTimeKind.Utc, dt.Kind);
        Assert.Equal(14, dt.Hour);
    }

    [Fact]
    public void GivenNonUtcDateTimeOffset_WhenProjectingToDateTime_ThenReturnsUnspecifiedKind()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var dt = (DateTime)DateTimeProjection.Project(source, typeof(DateTime));

        // Assert
        Assert.Equal(DateTimeKind.Unspecified, dt.Kind);
        Assert.Equal(14, dt.Hour);
    }

    [Fact]
    public void GivenUnsupportedTargetType_WhenProjecting_ThenThrows()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            DateTimeProjection.Project(source, typeof(string)));
    }

    [Theory]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    public void GivenType_WhenCheckingIsTemporalType_ThenReturnsExpected(Type type, bool expected)
    {
        // Act
        var result = DateTimeProjection.IsTemporalType(type);

        // Assert
        Assert.Equal(expected, result);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToDateOnly_ThenExtractsDate()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.FromHours(2));

        // Act
        var date = (DateOnly)DateTimeProjection.Project(source, typeof(DateOnly));

        // Assert
        Assert.Equal(new DateOnly(2024, 1, 15), date);
    }

    [Fact]
    public void GivenDateTimeOffset_WhenProjectingToTimeOnly_ThenExtractsTime()
    {
        // Arrange
        var source = new DateTimeOffset(2024, 1, 15, 14, 30, 45, TimeSpan.FromHours(2));

        // Act
        var time = (TimeOnly)DateTimeProjection.Project(source, typeof(TimeOnly));

        // Assert
        Assert.Equal(new TimeOnly(14, 30, 45), time);
    }

    [Theory]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(TimeOnly), true)]
    public void GivenNet6TemporalType_WhenCheckingIsTemporalType_ThenReturnsTrue(Type type, bool expected)
    {
        // Act
        var result = DateTimeProjection.IsTemporalType(type);

        // Assert
        Assert.Equal(expected, result);
    }
#endif
}
