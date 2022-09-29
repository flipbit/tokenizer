using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tokens.Logging;

public class LogProvider
{
    public static LogLevel LogLevel = LogLevel.Information;
    public static ILoggerFactory Factory = new NullLoggerFactory();
    public static ILogger<T> For<T>()
    {
        return Factory.CreateLogger<T>();
    }
}