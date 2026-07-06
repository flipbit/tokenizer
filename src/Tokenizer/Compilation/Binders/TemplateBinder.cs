using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Binds a parsed TemplateDocument AST (syntax only) to a TemplateDefinition structure.
/// </summary>
internal static class TemplateBinder
{
    public static TemplateDefinition Bind(TemplateDocument document)
    {
        var result = new TemplateDefinition();
        var tokens = new List<TokenDefinition>();
        var preambleBuilder = new System.Text.StringBuilder();

        // Read relevant front matter options to influence binding
        var globalTrimPreambleBeforeNewLine = IsFrontMatterOptionTrue(document, "trimpreamblebeforenewline");
        var globalTerminateOnNewLine = IsFrontMatterOptionTrue(document, "terminateonnewline");

        foreach (var node in document.Content)
        {
            if (node is TokenNode tokenNode)
            {
                var def = new TokenDefinition
                {
                    Location = tokenNode.Location.Clone(),
                    IsOptional = tokenNode.Modifiers.IsOptional,
                    IsRepeating = tokenNode.Modifiers.IsRepeating,
                    IsRequired = tokenNode.Modifiers.IsRequired,
                    TerminateOnNewLine = tokenNode.Modifiers.IsTerminate,
                };

                // Derived semantics: repeating tokens are optional (can match 0 times)
                if (def.IsRepeating)
                {
                    def.IsOptional = true;
                }
                var optionalExplicit = tokenNode.Modifiers.IsOptional;

                // Attach accumulated preamble before this token
                if (preambleBuilder.Length > 0)
                {
                    var pre = preambleBuilder.ToString();
                    if (globalTrimPreambleBeforeNewLine && pre.IndexOf('\n') > -1)
                    {
                        pre = pre.Substring(pre.LastIndexOf('\n') + 1);
                    }
                    def.AppendPreamble(pre);
                    preambleBuilder.Clear();
                }

                def.AppendName(tokenNode.Name.Text);
                // Null-token semantics match legacy: name "null" (any case) marks token as null
                def.IsNull = string.Equals(tokenNode.Name.Text, "null", StringComparison.InvariantCultureIgnoreCase);
                // Preserve raw token content for ToString() parity
                def.Content = BuildRawTokenContent(tokenNode);
                if (tokenNode.Value != null)
                {
                    def.AppendValue(tokenNode.Value.Text);
                }

                // Map decorators and arguments, with special longhand modifiers
                var decorators = new List<DecoratorDefinition>();
                foreach (var dec in tokenNode.Decorators)
                {
                    var decoratorName = (dec.Name.Text ?? string.Empty).Trim();
                    var lower = decoratorName.ToLowerInvariant();
                    var hasArgs = dec.Args != null && dec.Args.Count > 0;

                    // Special longhand forms without args
                    if (!hasArgs)
                    {
                        if (lower == "eol" || lower == "$")
                        {
                            def.TerminateOnNewLine = true;
                            continue;
                        }
                        if (lower == "optional" || lower == "?")
                        {
                            def.IsOptional = true;
                            optionalExplicit = true;
                            continue;
                        }
                        if (lower == "repeating" || lower == "*")
                        {
                            def.IsRepeating = true;
                            def.IsOptional = true; // repeating implies optional, but not explicit
                            continue;
                        }
                        if (lower == "required" || lower == "!")
                        {
                            def.IsRequired = true;
                            continue;
                        }
                        // Longhand modifier: Once => IsSingleUse semantics
                        if (lower == "once")
                        {
                            def.IsSingleUse = true;
                            continue;
                        }
                    }

                    var d = new DecoratorDefinition();
                    d.AppendName(decoratorName);
                    d.IsNotDecorator = dec.IsNot;
                    foreach (var arg in dec?.Args ?? System.Array.Empty<Nodes.ArgumentNode>())
                    {
                        d.Args.Add(arg.Text);
                    }
                    decorators.Add(d);
                }
                def.AppendDecorators(decorators);

                // Validate incompatible modifiers: only when optional is explicit (not via repeating)
                if (def.IsRequired && optionalExplicit)
                {
                    throw new ParsingException($"Optional token {def.Name} can't be Required", def.Location);
                }

                // Apply global terminate option if set in front matter
                if (globalTerminateOnNewLine)
                {
                    def.TerminateOnNewLine = true;
                }

                // Legacy behavior: expand repeating token with multiline preamble tail
                var repeatingTail = GetRepeatingMultilinePreamble(def);
                if (def.IsRepeating && repeatingTail is { Length: > 0 })
                {
                    // First token becomes non-repeating, keeps original preamble
                    def.IsRepeating = false;
                    tokens.Add(def);

                    // Second token repeats with preamble set to the tail ("\n" + whitespace)
                    var repeat = new TokenDefinition
                    {
                        Location = def.Location.Clone(),
                        IsOptional = true,
                        IsRepeating = true,
                        TerminateOnNewLine = def.TerminateOnNewLine,
                        Content = def.Content,
                    };
                    repeat.AppendName(def.Name);
                    repeat.AppendPreamble(repeatingTail);
                    repeat.AppendDecorators(def.Decorators);
                    tokens.Add(repeat);
                }
                else
                {
                    tokens.Add(def);
                }
            }
            else if (node is TextNode text)
            {
                // Accumulate preamble until next token
                preambleBuilder.Append(text.Text);
            }
        }

        // Legacy behavior: emit a terminal empty-name token only when there is
        // trailing preamble containing non-whitespace content after the last token.
        var trailingPreamble = preambleBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(trailingPreamble))
        {
            var tail = new TokenDefinition();
            tail.AppendName(string.Empty);
            if (globalTrimPreambleBeforeNewLine && trailingPreamble.IndexOf('\n') > -1)
            {
                trailingPreamble = trailingPreamble.Substring(trailingPreamble.LastIndexOf('\n') + 1);
            }
            tail.AppendPreamble(trailingPreamble);
            tokens.Add(tail);
        }

        foreach (var t in tokens)
        {
            result.Tokens.Add(t);
        }
        return result;
    }

    private static string BuildRawTokenContent(TokenNode node)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        // leading space before name to match legacy formatting
        sb.Append(' ');
        // name
        sb.Append(node.Name.Text);
        // inline modifiers in symbol form
        if (node.Modifiers.IsOptional) sb.Append('?');
        if (node.Modifiers.IsTerminate) sb.Append('$');
        if (node.Modifiers.IsRepeating) sb.Append('*');
        if (node.Modifiers.IsRequired) sb.Append('!');
        // value
        if (node.Value != null)
        {
            sb.Append('=');
            sb.Append(node.Value.IsQuoted ? $"'{node.Value.Text}'" : node.Value.Text);
        }
        // decorators: join groups with colons; within group, comma-separated
        if (node.Decorators != null && node.Decorators.Count > 0)
        {
            sb.Append(" : ");
            for (int i = 0; i < node.Decorators.Count; i++)
            {
                var d = node.Decorators[i];
                if (d.IsNot) sb.Append('!');
                sb.Append(d.Name.Text);
                if (d.Args != null && d.Args.Count > 0)
                {
                    sb.Append('(');
                    for (int j = 0; j < d.Args.Count; j++)
                    {
                        var a = d.Args[j];
                        sb.Append(a.IsQuoted ? $"'{a.Text}'" : a.Text);
                        if (j < d.Args.Count - 1) sb.Append(", ");
                    }
                    sb.Append(')');
                }
                if (i < node.Decorators.Count - 1) sb.Append(", ");
            }
        }
        // trailing space before closing brace to match expected string
        sb.Append(' ');
        sb.Append("}");
        return sb.ToString();
    }

    private static string? GetRepeatingMultilinePreamble(TokenDefinition token)
    {
        if (!token.IsRepeating) return null;
        if (string.IsNullOrEmpty(token.Preamble)) return null;
        if (token.Preamble.IndexOf('\n') == -1) return null;

        var pre = token.Preamble.SubstringBeforeLastString("\n");
        var post = token.Preamble.SubstringAfterLastString("\n");

        if (!string.IsNullOrWhiteSpace(pre) &&
            string.IsNullOrWhiteSpace(post))
        {
            return "\n" + post;
        }

        return null;
    }

    private static bool IsFrontMatterOptionTrue(TemplateDocument document, string key)
    {
        if (document?.FrontMatter == null) return false;
        foreach (var entry in document.FrontMatter.Entries)
        {
            if (entry is FrontMatterEntry e)
            {
                var k = (e.Key ?? string.Empty).Trim().ToLowerInvariant();
                if (k == key)
                {
                    var v = (e.Value ?? string.Empty).Trim().ToLowerInvariant();
                    if (v == "true" || v == "yes" || v == "on") return true;
                    if (v == "false" || v == "no" || v == "off") return false;
                }
            }
        }
        return false;
    }
}



