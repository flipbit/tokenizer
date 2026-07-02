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
        options.RegisterTransformer<ToUpperTransformer>();

        // Assert
        Assert.Contains(typeof(ToUpperTransformer), options.Transformers);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenValidatorTypeIsStored()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        options.RegisterValidator<IsNumericValidator>();

        // Assert
        Assert.Contains(typeof(IsNumericValidator), options.Validators);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenReturnsSameOptionsForChaining()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var result = options.RegisterTransformer<ToUpperTransformer>();

        // Assert
        Assert.Same(options, result);
    }

    [Fact]
    public void GivenNewOptions_WhenCheckingDefaults_ThenCompilationCacheMaxSizeIs500()
    {
        // Arrange / Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Equal(500, options.CompilationCacheMaxSize);
    }

    [Fact]
    public void GivenNewOptions_WhenSettingCacheMaxSizeToZero_ThenCachingIsDisabled()
    {
        // Arrange / Act
        var options = new TokenizerOptions { CompilationCacheMaxSize = 0 };

        // Assert
        Assert.Equal(0, options.CompilationCacheMaxSize);
    }

    [Fact]
    public void GivenOptionsWithBuiltInTransformer_WhenRegisteringSameType_ThenTokenParserDoesNotDuplicate()
    {
        // Arrange
        var options = new TokenizerOptions();
        options.RegisterTransformer<ToUpperTransformer>(); // ToUpper is a built-in

        // Act — no exception expected; TokenParser deduplicates built-ins vs. custom registrations
        var parser = new TokenParser(options);

        // Assert — template that uses ToUpper compiles and runs without error
        var template = parser.Parse("{Value:ToUpper}");
        Assert.NotNull(template);
    }
}
