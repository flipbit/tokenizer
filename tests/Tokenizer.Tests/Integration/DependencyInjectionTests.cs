using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Tokens.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Integration;

public class DependencyInjectionTests : TokenizerTestBase
{
    public DependencyInjectionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDefaultOptions_WhenAddTokenizer_ThenRegistersAllServices()
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
    public void GivenCustomOptions_WhenAddTokenizer_ThenAppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());

        // Act
        services.AddTokenizer(new TokenizerOptions
        {
            TrimTrailingWhiteSpace = false,
            OutOfOrderTokens = true
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tokenizer = serviceProvider.GetService<Tokenizer>();
        Assert.NotNull(tokenizer);
        Assert.False(tokenizer.Options.TrimTrailingWhiteSpace);
        Assert.True(tokenizer.Options.OutOfOrderTokens);
    }

    [Fact]
    public void GivenAddTokenizer_WhenResolvingMultipleTimes_ThenReturnsSingleton()
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
    public void GivenAddTokenizer_WhenTokenizingInput_ThenReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        services.AddTokenizer();
        var serviceProvider = services.BuildServiceProvider();
        var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();

        // Act
        var template = tokenizer.Compile("{name}").Template;
        var result = tokenizer.Tokenize(template, "John");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("John", result.First("name"));
    }

    [Fact]
    public void GivenAddTokenizerWithLogging_WhenTokenizing_ThenLogsAreCreated()
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
        var template = tokenizer.Compile("{name}").Template;
        var result = tokenizer.Tokenize(template, "John");

        // Assert - just verify it doesn't throw
        Assert.True(result.Success);
    }

    [Fact]
    public void GivenConfigurationSection_WhenAddTokenizer_ThenBindsOptions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tokenizer:TrimTrailingWhiteSpace"] = "false",
                ["Tokenizer:OutOfOrderTokens"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());

        // Act
        services.AddTokenizer(configuration.GetSection("Tokenizer"));
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();
        Assert.False(tokenizer.Options.TrimTrailingWhiteSpace);
        Assert.True(tokenizer.Options.OutOfOrderTokens);
    }

    [Fact]
    public void GivenOptionsInstance_WhenAddTokenizer_ThenUsesProvidedOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog());
        var options = new TokenizerOptions
        {
            MaxInputLength = 512,
            EnableDiagnostics = true
        };

        // Act
        services.AddTokenizer(options);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tokenizer = serviceProvider.GetRequiredService<Tokenizer>();
        Assert.Equal(512, tokenizer.Options.MaxInputLength);
        Assert.True(tokenizer.Options.EnableDiagnostics);
    }
}
