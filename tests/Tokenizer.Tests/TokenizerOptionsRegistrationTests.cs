using Tokens.Compilation;
using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens;

public class TokenizerOptionsRegistrationTests
{
    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenTransformerTypeIsStored()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.WithTransformer<ToUpperTransformer>();

        // Assert
        Assert.Contains(typeof(ToUpperTransformer), result.Transformers);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenValidatorTypeIsStored()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.WithValidator<IsNumericValidator>();

        // Assert
        Assert.Contains(typeof(IsNumericValidator), result.Validators);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenReturnsNewInstance()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.WithTransformer<ToUpperTransformer>();

        // Assert — WithTransformer returns a new instance (immutability)
        Assert.NotSame(options, result);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenOriginalIsUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        options.WithTransformer<ToUpperTransformer>();

        // Assert — original options are not mutated
        Assert.DoesNotContain(typeof(ToUpperTransformer), options.Transformers);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenReturnsNewInstance()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.WithValidator<IsNumericValidator>();

        // Assert — WithValidator returns a new instance (immutability)
        Assert.NotSame(options, result);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenOriginalIsUnchanged()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        options.WithValidator<IsNumericValidator>();

        // Assert — original options are not mutated
        Assert.DoesNotContain(typeof(IsNumericValidator), options.Validators);
    }

    [Fact]
    public void GivenOptionsWithBuiltInTransformer_WhenRegisteringSameType_ThenTemplateCompilerDoesNotDuplicate()
    {
        // Arrange
        var options = new TokenizerOptions().WithTransformer<ToUpperTransformer>(); // ToUpper is a built-in

        // Act — no exception expected; TemplateCompiler deduplicates built-ins vs. custom registrations
        var parser = new TemplateCompiler(options);

        // Assert — template that uses ToUpper compiles and runs without error
        var template = parser.Compile("{Value:ToUpper}");
        Assert.NotNull(template);
    }
}
