using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tokens.Tokenization;

namespace Tokens.Extensions;

/// <summary>
/// Extension methods for configuring Tokenizer services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class TokenizerServiceCollectionExtensions
{
    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/> with default options.
    /// </summary>
    public static IServiceCollection AddTokenizer(this IServiceCollection services)
    {
        return services.AddTokenizer(_ => { });
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// configured via the provided delegate.
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        Action<TokenizerOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// bound to a configuration section (e.g. from appsettings.json).
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        services.Configure<TokenizerOptions>(configuration);
        RegisterCoreServices(services);
        return services;
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/>
    /// using a pre-constructed <see cref="TokenizerOptions"/> instance.
    /// </summary>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        TokenizerOptions options)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (options == null) throw new ArgumentNullException(nameof(options));

        services.AddSingleton(Options.Create(options));
        RegisterCoreServices(services);
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton<Compilation.TokenParser>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<TokenizerOptions>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Compilation.TokenParser>();
            return new Compilation.TokenParser(opts.Value, logger);
        });

        services.TryAddSingleton<ITokenizationEngine>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TokenizationEngine>();
            return new TokenizationEngine(logger);
        });

        services.TryAddSingleton<IResultBuilder>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ResultBuilder>();
            return new ResultBuilder(logger);
        });

        services.TryAddSingleton<ITokenizer>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<TokenizerOptions>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Tokenizer>();
            var parser = sp.GetRequiredService<Compilation.TokenParser>();
            var tokenizationEngine = sp.GetRequiredService<ITokenizationEngine>();
            var resultBuilder = sp.GetRequiredService<IResultBuilder>();

            return new Tokenizer(opts, logger, parser, tokenizationEngine, resultBuilder);
        });

        services.TryAddSingleton<Tokenizer>(sp => (Tokenizer)sp.GetRequiredService<ITokenizer>());

        services.TryAddSingleton<ITokenMatcher>(sp =>
        {
            var tokenizer = sp.GetRequiredService<ITokenizer>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new TokenMatcher(tokenizer, loggerFactory);
        });
    }
}
