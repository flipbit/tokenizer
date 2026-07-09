using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace Tokens;

/// <summary>
/// Factory for creating loggers that automatically output to XUnit test output.
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>
    /// Creates an ILogger instance that writes to XUnit test output.
    /// </summary>
    /// <typeparam name="T">The type to create the logger for</typeparam>
    /// <param name="output">The XUnit test output helper</param>
    /// <returns>An ILogger instance configured to write to test output</returns>
    public static ILogger<T> Create<T>(ITestOutputHelper output)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(output)
            .CreateLogger();

        using var loggerFactory = new SerilogLoggerFactory(serilogLogger);
        return loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Creates an ILoggerFactory that writes to XUnit test output.
    /// </summary>
    /// <param name="output">The XUnit test output helper</param>
    /// <returns>An ILoggerFactory instance configured to write to test output</returns>
    public static ILoggerFactory CreateFactory(ITestOutputHelper output)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(output)
            .CreateLogger();

        return new SerilogLoggerFactory(serilogLogger);
    }
}
