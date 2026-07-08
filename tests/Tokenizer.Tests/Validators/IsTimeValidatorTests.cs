#if NET6_0_OR_GREATER
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsTimeValidatorTests : TokenizerTestBase
{
    public IsTimeValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsTimeValidator _validator = new();

    [Fact]
    public void GivenTimeOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange / Act / Assert
        Assert.True(_validator.IsValid("14:30:00"));
    }

    [Fact]
    public void GivenDateTimeString_WhenValidating_ThenReturnsFalse()
    {
        // IsTime rejects values with date components
        Assert.False(_validator.IsValid("2024-01-15 14:30:00"));
    }

    [Fact]
    public void GivenInvalidString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange / Act / Assert
        Assert.False(_validator.IsValid("hello"));
    }
}
#endif
