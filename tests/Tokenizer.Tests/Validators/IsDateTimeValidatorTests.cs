using Xunit;

namespace Tokens.Validators;

public class IsDateTimeValidatorTests
{
    private readonly IsDateTimeValidator validator = new();

    [Fact]
    public void TestValidateValueWhenValid()
    {
        var result = validator.IsValid("1 May 2019");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenNewIsoDate()
    {
        var result = validator.IsValid("2019-05-01");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenHasTime()
    {
        var result = validator.IsValid("2019-05-01 14:00:00");

        Assert.True(result);
    }

    [Fact]
    public void TestValidateValueWhenInvalid()
    {
        var result = validator.IsValid("hello world");

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenNull()
    {
        var result = validator.IsValid(null);

        Assert.False(result);
    }

    [Fact]
    public void TestValidateValueWhenEmpty()
    {
        var result = validator.IsValid(string.Empty);

        Assert.False(result);
    }

    [Fact]
    public void TestForDocumentation()
    {
        var template = "Date: { Date : IsDateTime('yyyy-MM-dd') }";
        var input = "Date: 3rd Oct 2019 Date: 2019-10-04";

        var result = new Tokenizer().Tokenize(template, input);

        Assert.Equal("2019-10-04", result.First("Date"));
    }
}