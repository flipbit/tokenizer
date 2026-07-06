namespace Tokens.Tokenization;

internal static class ArgumentValidation
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object argument, string paramName)
    {
        if (argument == null) throw new ArgumentNullException(paramName);
    }
#else
    public static void ThrowIfNull(object argument, string paramName)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
#endif
}
