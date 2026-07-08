#if NET6_0_OR_GREATER
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsDateValidatorTests : TokenizerTestBase
{
    public IsDateValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsDateValidator _validator = new();

    [Fact]
    public void GivenDateOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange / Act / Assert
        Assert.True(_validator.IsValid("2024-01-15"));
    }

    [Fact]
    public void GivenDateTimeString_WhenValidating_ThenReturnsFalse()
    {
        // IsDate rejects values with time components
        Assert.False(_validator.IsValid("2024-01-15 14:30:00"));
    }

    [Fact]
    public void GivenInvalidString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange / Act / Assert
        Assert.False(_validator.IsValid("hello"));
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange / Act / Assert
        Assert.False(_validator.IsValid(null!));
    }
}
#endif
