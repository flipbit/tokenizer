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
        Assert.Equal(25, registry.Transformers.Count);
        Assert.Contains(registry.Transformers, r => r.Type == typeof(ToDateTimeTransformer));
        Assert.Contains(registry.Transformers, r => r.Type == typeof(ToUpperTransformer));
        Assert.Contains(registry.Transformers, r => r.Type == typeof(SetTransformer));
    }

    [Fact]
    public void GivenDefaultOptions_WhenCreated_ThenDiscoverAllBuiltInValidators()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.Equal(23, registry.Validators.Count);
        Assert.Contains(registry.Validators, r => r.Type == typeof(IsNumericValidator));
        Assert.Contains(registry.Validators, r => r.Type == typeof(IsEmailValidator));
        Assert.Contains(registry.Validators, r => r.Type == typeof(MatchesRegexValidator));
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
        Assert.Equal(26, registry.Transformers.Count);
        Assert.Contains(registry.Transformers, r => r.Type == typeof(StubTransformer));
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
        Assert.Equal(24, registry.Validators.Count);
        Assert.Contains(registry.Validators, r => r.Type == typeof(StubValidator));
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
        Assert.Equal(25, registry.Transformers.Count);
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
        Assert.Equal(23, registry.Validators.Count);
    }

    [Fact]
    public void GivenDefaultOptions_WhenCreated_ThenOnlyDiscoversConcreteTypes()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        Assert.DoesNotContain(registry.Transformers, r => r.Type == typeof(ITokenTransformer));
        Assert.DoesNotContain(registry.Validators, r => r.Type == typeof(ITokenValidator));
    }

    [Fact]
    public void GivenBuiltInDecorators_WhenComparedToAssemblyScan_ThenAllConcreteTypesAreRegistered()
    {
        // Arrange
        var assembly = typeof(ITokenTransformer).Assembly;
        var concreteTransformers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenTransformer).IsAssignableFrom(t))
            .ToList();
        var concreteValidators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenValidator).IsAssignableFrom(t))
            .ToList();
        var options = new TokenizerOptions();

        // Act
        var registry = new DecoratorRegistry(options);

        // Assert
        foreach (var type in concreteTransformers)
        {
            Assert.Contains(registry.Transformers, r => r.Type == type);
        }

        foreach (var type in concreteValidators)
        {
            Assert.Contains(registry.Validators, r => r.Type == type);
        }
    }

    private sealed class StubTransformer : ITokenTransformer
    {
        public bool TryTransform(object value, string[] args, out object transformed)
        {
            transformed = value;
            return false;
        }
    }

    private sealed class StubValidator : ITokenValidator
    {
        public bool IsValid(object value, params string[] args) => true;
    }
}
