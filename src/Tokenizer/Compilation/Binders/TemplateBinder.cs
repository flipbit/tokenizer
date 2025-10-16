using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation.Binders
{
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
            var globalTerminateOnNewline = IsFrontMatterOptionTrue(document, "terminateonnewline");

            foreach (var node in document.Content)
            {
                if (node is TokenNode tokenNode)
                {
                    var def = new TokenDefinition
                    {
                        Location = tokenNode.Location.Clone(),
                        Optional = tokenNode.Modifiers.IsOptional,
                        Repeating = tokenNode.Modifiers.IsRepeating,
                        Required = tokenNode.Modifiers.IsRequired,
                        TerminateOnNewline = tokenNode.Modifiers.IsTerminate
                    };

                    // Derived semantics: repeating tokens are optional (can match 0 times)
                    if (def.Repeating)
                    {
                        def.Optional = true;
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
                        var decoratorName = (dec.Name?.Text ?? string.Empty).Trim();
                        var lower = decoratorName.ToLowerInvariant();
                        var hasArgs = dec.Args != null && dec.Args.Count > 0;

                        // Special longhand forms without args
                        if (hasArgs == false)
                        {
                            if (lower == "eol" || lower == "$")
                            {
                                def.TerminateOnNewline = true;
                                continue;
                            }
                            if (lower == "optional" || lower == "?")
                            {
                                def.Optional = true;
                                optionalExplicit = true;
                                continue;
                            }
                            if (lower == "repeating" || lower == "*")
                            {
                                def.Repeating = true;
                                def.Optional = true; // repeating implies optional, but not explicit
                                continue;
                            }
                            if (lower == "required" || lower == "!")
                            {
                                def.Required = true;
                                continue;
                            }
                            // Longhand modifier: Once => ConsiderOnce semantics
                            if (lower == "once")
                            {
                                def.ConsiderOnce = true;
                                continue;
                            }
                        }

                        var d = new DecoratorDefinition();
                        d.AppendName(decoratorName);
                        d.IsNotDecorator = dec.IsNot;
                        foreach (var arg in dec.Args)
                        {
                            d.Args.Add(arg.Text);
                        }
                        decorators.Add(d);
                    }
                    def.AppendDecorators(decorators);

                    // Validate incompatible modifiers: only when optional is explicit (not via repeating)
                    if (def.Required && optionalExplicit)
                    {
                        throw new ParsingException($"Optional token {def.Name} can't be Required", def.Location);
                    }

                    // Apply global terminate option if set in front matter
                    if (globalTerminateOnNewline)
                    {
                        def.TerminateOnNewline = true;
                    }

                    // Legacy behavior: expand repeating token with multiline preamble tail
                    var repeatingTail = GetRepeatingMultilinePreamble(def);
                    if (def.Repeating && string.IsNullOrEmpty(repeatingTail) == false)
                    {
                        // First token becomes non-repeating, keeps original preamble
                        def.Repeating = false;
                        tokens.Add(def);

                        // Second token repeats with preamble set to the tail ("\n" + whitespace)
                        var repeat = new TokenDefinition
                        {
                            Location = def.Location.Clone(),
                            Optional = true,
                            Repeating = true,
                            TerminateOnNewline = def.TerminateOnNewline,
                            Content = def.Content
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
            if (string.IsNullOrWhiteSpace(trailingPreamble) == false)
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
            // Assign sequential, 1-based Ids to all tokens to preserve stable ordering semantics
            for (int i = 0; i < result.Tokens.Count; i++)
            {
                result.Tokens[i].Id = i + 1;
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

        private static string GetRepeatingMultilinePreamble(TokenDefinition token)
        {
            if (token.Repeating == false) return null;
            if (string.IsNullOrEmpty(token.Preamble)) return null;
            if (token.Preamble.IndexOf('\n') == -1) return null;

            var pre = token.Preamble.SubstringBeforeLastString("\n");
            var post = token.Preamble.SubstringAfterLastString("\n");

            if (string.IsNullOrWhiteSpace(pre) == false &&
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
}



