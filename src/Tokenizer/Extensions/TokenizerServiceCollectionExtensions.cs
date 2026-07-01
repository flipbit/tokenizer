using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddTokenizer(this IServiceCollection services)
    {
        return services.AddTokenizer(_ => { });
    }

    /// <summary>
    /// Adds Tokenizer services to the specified <see cref="IServiceCollection"/> with custom configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An action to configure the <see cref="TokenizerOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddTokenizer(
        this IServiceCollection services,
        Action<TokenizerOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Create and configure options
        var options = new TokenizerOptions();
        configure(options);

        // Register options as singleton
        services.TryAddSingleton(options);

        // Register internal services as singletons
        services.TryAddSingleton<Compilation.TokenParser>(sp =>
        {
            var opts = sp.GetRequiredService<TokenizerOptions>();
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Compilation.TokenParser>();
            return new Compilation.TokenParser(opts, logger);
        });

        services.TryAddSingleton<ITokenizationEngine>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TokenizationEngine>();
            return new TokenizationEngine(logger);
        });

        services.TryAddSingleton<IHintProcessor>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<HintProcessor>();
            return new HintProcessor(logger);
        });

        services.TryAddSingleton<IResultBuilder>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ResultBuilder>();
            return new ResultBuilder(logger);
        });

        // Register main Tokenizer as singleton
        services.TryAddSingleton<Tokenizer>(sp =>
        {
            var opts = sp.GetRequiredService<TokenizerOptions>();
            var loggerFactory = sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Tokenizer>();
            var parser = sp.GetRequiredService<Compilation.TokenParser>();
            var tokenizationEngine = sp.GetRequiredService<ITokenizationEngine>();
            var hintProcessor = sp.GetRequiredService<IHintProcessor>();
            var resultBuilder = sp.GetRequiredService<IResultBuilder>();

            return new Tokenizer(opts, logger, parser, tokenizationEngine, hintProcessor, resultBuilder);
        });

        return services;
    }
}
