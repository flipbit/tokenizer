using System.Text;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

/// <summary>
/// Renders a chronological walk-through of every engine decision during tokenization.
/// Each raw event is numbered and formatted with its key properties.
/// </summary>
internal static class ProcessingOrderRenderer
{
    public static string Render(TokenizationDiagnostics diagnostics)
    {
        var sb = new StringBuilder();
        var events = diagnostics.RawEvents;

        sb.AppendLine("═══ Processing Order ═══");
        sb.Append(events.Count.ToInvariant()).AppendLine(" events recorded");
        sb.AppendLine();

        for (var i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            sb.Append('[').Append((i + 1).ToInvariant()).Append("] ").Append(evt.Type);

            if (evt.TokenName != null)
                sb.Append(" — ").Append(evt.TokenName);

            if (evt.Location != null)
                sb.Append(" (line ").Append(evt.Location.Line.ToInvariant()).Append(')');

            sb.AppendLine();

            // Indent detail lines
            if (evt.Value != null)
                sb.Append("      Value: ").AppendLine(evt.Value);

            if (evt.DecoratorName != null)
                sb.Append("      Decorator: ").Append(evt.DecoratorName);

            if (evt.DecoratorArgs != null && evt.DecoratorArgs.Length > 0)
#if NETSTANDARD2_0
                sb.Append('(').Append(string.Join(", ", evt.DecoratorArgs)).Append(')');
#else
                sb.Append('(').AppendJoin(", ", evt.DecoratorArgs).Append(')');
#endif

            if (evt.DecoratorName != null)
                sb.AppendLine();

            if (evt.Detail != null)
                sb.Append("      Detail: ").AppendLine(evt.Detail);
        }

        return sb.ToString();
    }
}
