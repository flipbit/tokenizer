using Xunit;

namespace Tokens.Exceptions;

public class ValidationExceptionTests
{
    [Fact]
    public void GivenValidationException_WhenCreated_ThenInheritsFromTokenizerException()
    {
        var exception = new ValidationException("test");
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }

    [Fact]
    public void GivenValidationException_WhenCaughtAsTokenizerException_ThenIsCaught()
    {
        TokenizerException? caught;

        try
        {
            throw new ValidationException("test");
        }
        catch (TokenizerException ex)
        {
            caught = ex;
        }

        Assert.NotNull(caught);
        Assert.IsType<ValidationException>(caught);
    }

    [Fact]
    public void GivenValidationExceptionWithInner_WhenCreated_ThenPreservesInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new ValidationException("outer", inner);
        Assert.IsAssignableFrom<TokenizerException>(exception);
        Assert.Same(inner, exception.InnerException);
        Assert.Equal("outer", exception.Message);
    }
}
