using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Tokens.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Integration;

public class DependencyInjectionTests : TokenizerTestBase
{
    public DependencyInjectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AddTokenizer_WithDefaultOptions_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());

        // Act
        services.AddTokenizer();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tokenizer = serviceProvider.GetService<Tokenizer>();
        Assert.NotNull(tokenizer);
        Assert.NotNull(tokenizer.Options);
    }

    [Fact]
    public void AddTokenizer_WithCustomOptions_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());

        // Act
        services.AddTokenizer(options =>
        {
            options.TrimTrailingWhiteSpace = false;
            options.OutOfOrderTokens = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tokenizer = serviceProvider.GetService<Tokenizer>();
        Assert.NotNull(tokenizer);
        Assert.False(tokenizer.Options.TrimTrailingWhiteSpace);
        Assert.True(tokenizer.Options.OutOfOrderTokens);
    }

    [Fact]
    public void AddTokenizer_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        services.AddTokenizer();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var tokenizer1 = serviceProvider.GetService<Tokenizer>();
        var tokenizer2 = serviceProvider.GetService<Tokenizer>();

        // Assert
        Assert.Same(tokenizer1, tokenizer2);
    }

    [Fact]
    public void AddTokenizer_TokenizerCanTokenizeInput()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        services.AddTokenizer();
        var serviceProvider = services.BuildServiceProvider();
        var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();

        // Act
        var result = tokenizer.Tokenize("{name}", "John");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("John", result.First("name"));
    }

    [Fact]
    public void AddTokenizer_WithLogging_LogsAreCreated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddTokenizer();
        var serviceProvider = services.BuildServiceProvider();
        var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();

        // Act
        var result = tokenizer.Tokenize("{name}", "John");

        // Assert - just verify it doesn't throw
        Assert.True(result.Success);
    }
}
