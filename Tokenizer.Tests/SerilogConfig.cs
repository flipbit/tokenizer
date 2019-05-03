using Serilog;

namespace Tokens
{
    class SerilogConfig
    {
        public static void Init()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel
                .Verbose()
                .WriteTo
                .Console()
                .CreateLogger();
        }
    }
}
