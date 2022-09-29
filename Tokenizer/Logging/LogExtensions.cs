using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tokens.Logging
{
    static class VerboseLogExtensions
    {
        public static void Verbose(this ILogger log, string message, params object[] args)
        {
            try
            {
                log.LogTrace(string.Format(string.Concat(LogIndentation.Indentation, message), args));
            }
            catch (System.FormatException e)
            {
                log.LogWarning($"String has incorrect format: '{message}'");
            }
        }
 
        public static void Verbose(this ILogger log, Exception exception, string message, params object[] args)
        {
            try
            {
                log.LogError(string.Format(string.Concat(LogIndentation.Indentation, message), exception, args));
            }
            catch (System.FormatException e)
            {
                log.LogWarning($"String has incorrect format: '{message}'");
            }
        }
 
        public static bool IsDebugEnabled(this ILogger log)
        {
            return LogProvider.LogLevel == LogLevel.Debug || LogProvider.LogLevel == LogLevel.Trace;
        }
    }
}
