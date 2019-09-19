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
        private static StringBuilder sb = new StringBuilder();

        public static void Write(TokenizeResult result)
        {
            sb.Clear();

            var listNames = new List<string>(); 

            foreach (var match in result.Matches)
            {
                var name = match.Token.Name;

                if (result.Matches.Count(m => m.Token.Name == name) > 1)
                {
                    if (listNames.Contains(name)) continue;

                    var listMatches = result.All(name);
                    var listCount = 0;

                    sb.AppendLine();
                        WriteValue($@"result.All(""{name}"").Count", listMatches.Count);
                    foreach (var listMatch in listMatches)
                    {
                        WriteValue($@"result.All(""{name}"")[{listCount}]", listMatch);

                        listCount++;
                    }
                    sb.AppendLine();

                    listNames.Add(name);
                }
                else
                {
                    WriteValue($@"result.First(""{name}"")", match.Value);
                }
            }

            Console.Write(sb.ToString());
            WindowsClipboard.SetText(sb.ToString());
        }

        private static void WriteValue(string name, object value)
        {

            if (value is string)
            {
                sb.AppendLine($@"            Assert.AreEqual(""{value}"", {name});");
            }

            if (value is int)
            {
                sb.AppendLine($@"            Assert.AreEqual({value}, {name});");
            }

            if (value is DateTime dateTime)
            {
                sb.AppendLine($@"            Assert.AreEqual(new DateTime({dateTime.Year}, {dateTime.Month:00}, {dateTime.Day:00}, {dateTime.Hour:00}, {dateTime.Minute:00}, {dateTime.Second:00}, {dateTime.Millisecond:000}, DateTimeKind.Utc), {name});");
                    
            }
        }
    }
}
