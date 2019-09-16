using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tokens
{
    /// <summary>
    /// Helper class to write out the expected assertions for a unit test
    /// </summary>
    internal class AssertWriter
    {
        public static void Write(TokenMatcherResult result)
        {
            if (result.BestMatch == null) return;

            var keys = result
                .BestMatch
                .Matches
                .Select(m => m.Token.Name)
                .OrderBy(n => n);

            foreach (var key in keys)
            {
                var value = result.BestMatch.First(key);

                if (value is string)
                {
                    Console.WriteLine($@"            Assert.AreEqual(match.BestMatch.Values[""{key}""], ""{value}"");");
                }

                if (value is DateTime dateTime)
                {
                    Console.WriteLine($@"            Assert.AreEqual(match.BestMatch.Values[""{key}""], new DateTime({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second}));");
                        
                }

                /*
                if (value is List<object> list)
                {
                    Console.WriteLine($@"            Assert.AreEqual(match.BestMatch.Values[""{key}""], ""{value}"");");



                }
                */
            }
        }
    }
}
