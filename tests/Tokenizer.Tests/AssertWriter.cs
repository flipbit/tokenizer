using System.Text;

namespace Tokens;

/// <summary>
/// Helper class to write out the expected assertions for a unit test
/// </summary>
internal class AssertWriter
{
    private static readonly StringBuilder Sb = new StringBuilder();

    public static void Write(TokenizeResult result)
    {
        Sb.Clear();

        var listNames = new List<string>();

        foreach (var match in result.Matches)
        {
            var name = match.Token.Name;

            if (result.Matches.Count(m => m.Token.Name == name) > 1)
            {
                if (listNames.Contains(name)) continue;

                var listMatches = result.All(name);
                var listCount = 0;

                Sb.AppendLine();
                WriteValue($"""result.All("{name}").Count""", listMatches.Count);
                foreach (var listMatch in listMatches)
                {
                    WriteValue($"""result.All("{name}")[{listCount}]""", listMatch);

                    listCount++;
                }
                Sb.AppendLine();

                listNames.Add(name);
            }
            else
            {
                WriteValue($"""result.First("{name}")""", match.Value);
            }
        }

        Console.Write(Sb.ToString());
        WindowsClipboard.SetText(Sb.ToString());
    }

    private static void WriteValue(string name, object value)
    {

        if (value is string)
        {
            Sb.AppendLine($"""            Assert.Equal("{value}", {name});""");
        }

        if (value is int)
        {
            Sb.AppendLine($@"            Assert.Equal({value}, {name});");
        }

        if (value is DateTime dateTime)
        {
            Sb.AppendLine($@"            Assert.Equal(new DateTime({dateTime.Year}, {dateTime.Month:00}, {dateTime.Day:00}, {dateTime.Hour:00}, {dateTime.Minute:00}, {dateTime.Second:00}, {dateTime.Millisecond:000}, DateTimeKind.Utc), {name});");

        }
    }
}
