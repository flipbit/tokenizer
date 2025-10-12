using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tokens.Logging
{
    static class VerboseLogExtensions
    {
        public static void Verbose(this ILog log, string message, params object[] args)
        {
            log.TraceFormat(string.Concat(LogIndentation.Indentation, message), args);
        }
 
        public static void Verbose(this ILog log, Exception exception, string message, params object[] args)
        {
            log.TraceException(string.Concat(LogIndentation.Indentation, message), exception, args);
        }
    }
}
