using Tokens.Transformers;
using Tokens.Validators;
using Xunit;

namespace Tokens.Compilation;

public class DecoratorRegistryTests
{
    [Fact]
    public void GivenDefaultOptions_WhenCreated_ThenDiscoverAllBuiltInTransformers()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(23, registry.Transformers.Count);
        Assert.Contains(typeof(ToDateTimeTransformer), registry.Transformers);
        Assert.Contains(typeof(ToUpperTransformer), registry.Transformers);
        Assert.Contains(typeof(SetTransformer), registry.Transformers);
    }

    [Fact]
    public void GivenDefaultOptions_WhenCreated_ThenDiscoverAllBuiltInValidators()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(21, registry.Validators.Count);
        Assert.Contains(typeof(IsNumericValidator), registry.Validators);
        Assert.Contains(typeof(IsEmailValidator), registry.Validators);
        Assert.Contains(typeof(MatchesRegexValidator), registry.Validators);
    }

    [Fact]
    public void GivenOptionsWithCustomTransformer_WhenCreated_ThenIncludesCustomTransformer()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithTransformer<StubTransformer>();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(24, registry.Transformers.Count);
        Assert.Contains(typeof(StubTransformer), registry.Transformers);
    }

    [Fact]
    public void GivenOptionsWithCustomValidator_WhenCreated_ThenIncludesCustomValidator()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithValidator<StubValidator>();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(22, registry.Validators.Count);
        Assert.Contains(typeof(StubValidator), registry.Validators);
    }

    [Fact]
    public void GivenOptionsWithDuplicateBuiltInTransformer_WhenCreated_ThenNoDuplicates()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithTransformer<ToUpperTransformer>();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(23, registry.Transformers.Count);
    }

    [Fact]
    public void GivenOptionsWithDuplicateBuiltInValidator_WhenCreated_ThenNoDuplicates()
    {
        // Arrange
        var options = new TokenizerOptions()
            .WithValidator<IsNumericValidator>();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(21, registry.Validators.Count);
    }

    [Fact]
    public void GivenDefaultOptions_WhenCreated_ThenOnlyDiscoversConcreteTypes()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.DoesNotContain(typeof(ITokenTransformer), registry.Transformers);
        Assert.DoesNotContain(typeof(ITokenValidator), registry.Validators);
    }

    private class StubTransformer : ITokenTransformer
    {
        public bool TryTransform(object value, string[] args, out object transformed)
        {
            transformed = value;
            return false;
        }
    }

    private class StubValidator : ITokenValidator
    {
        public bool IsValid(object value, params string[] args) => true;
    }
}
