using System;
using System.Threading;

namespace Tokens.Logging
{
    internal class LogIndentation : IDisposable
    {
        private static int width;

        public LogIndentation()
        {
            Interlocked.Add(ref width, 2);
        }


        public void Dispose()
        {
            Interlocked.Add(ref width, -2);
        }

        public static string Indentation => new string(' ', Math.Abs(width));
    }
}