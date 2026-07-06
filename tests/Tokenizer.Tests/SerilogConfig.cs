using Serilog;

namespace Tokens;

internal class SerilogConfig
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
