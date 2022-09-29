
using Microsoft.Extensions.Logging;
using Tokens.Logging;

namespace Tokens
{
    class LogConfig
    {
        public static void Init()
        {
            // LoggerFactory should be disposed with the test case.
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole(options => options.DisableColors = true);
            });
            LogProvider.LogLevel = LogLevel.Trace;
            LogProvider.Factory = loggerFactory;
        }
    }
}
