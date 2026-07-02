using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens;

public class TokenizerOptionsRegistrationTests
{
    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenTransformerTypeIsStored()
    {
        var options = new TokenizerOptions();
        options.RegisterTransformer<ToUpperTransformer>();
        Assert.Contains(typeof(ToUpperTransformer), options.Transformers);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringValidator_ThenValidatorTypeIsStored()
    {
        var options = new TokenizerOptions();
        options.RegisterValidator<IsNumericValidator>();
        Assert.Contains(typeof(IsNumericValidator), options.Validators);
    }

    [Fact]
    public void GivenNewOptions_WhenRegisteringTransformer_ThenReturnsSameOptionsForChaining()
    {
        var options = new TokenizerOptions();
        var result = options.RegisterTransformer<ToUpperTransformer>();
        Assert.Same(options, result);
    }

    [Fact]
    public void GivenNewOptions_WhenCheckingDefaults_ThenCompilationCacheMaxSizeIs500()
    {
        var options = new TokenizerOptions();
        Assert.Equal(500, options.CompilationCacheMaxSize);
    }

    [Fact]
    public void GivenNewOptions_WhenSettingCacheMaxSizeToZero_ThenCachingIsDisabled()
    {
        var options = new TokenizerOptions { CompilationCacheMaxSize = 0 };
        Assert.Equal(0, options.CompilationCacheMaxSize);
    }
}
