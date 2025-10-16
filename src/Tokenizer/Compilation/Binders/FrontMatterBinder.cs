using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Compilation.Binders
{
    /// <summary>
    /// Binds front matter AST nodes onto <see cref="TemplateDefinition"/> and its <see cref="TokenizerOptions"/>.
    /// </summary>
    internal sealed class FrontMatterBinder
    {
        public void Bind(TemplateDefinition template, FrontMatterBlock frontMatter)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (frontMatter == null) return;
            // Ensure options are available for binding
            template.Options ??= TokenizerOptions.Defaults.Clone();

            foreach (var node in frontMatter.Entries)
            {
                switch (node)
                {
                    case FrontMatterComment:
                        // ignore comments
                        break;
                case SetTokenDirective dir:
                    ApplySetDirective(template, dir);
                        break;
                    case FrontMatterEntry entry:
                        ApplyOption(template, entry);
                        break;
                    default:
                        throw new ParsingException($"Unknown front matter node: {node.GetType().Name}", new FileLocation());
                }
            }
        }

        private static void ApplySetDirective(TemplateDefinition template, SetTokenDirective dir)
        {
            // Emulate v3 behavior: create a token with the given name and mark as front-matter token
            var token = new TokenDefinition
            {
                IsFrontMatterToken = true,
                Location = dir.Location.Clone()
            };
            token.AppendName(dir.TokenName);
            if (string.IsNullOrEmpty(dir.Value) == false)
            {
                token.AppendValue(dir.Value);
            }
            if (dir.Decorators.Count > 0)
            {
                foreach (var d in dir.Decorators)
                {
                    var dec = new DecoratorDefinition();
                    dec.AppendName(d.Name);
                    foreach (var a in d.Args)
                    {
                        dec.Args.Add(a);
                    }
                    token.Decorators.Add(dec);
                }
            }
            template.Tokens.Add(token);
        }

        private static void ApplyOption(TemplateDefinition template, FrontMatterEntry entry)
        {
            var key = (entry.Key ?? string.Empty).Trim().ToLowerInvariant();
            var rawName = entry.Key ?? string.Empty;
            var value = entry.Value ?? string.Empty;

            switch (key)
            {
                case "trimleadingwhitespace":
                    template.Options.TrimLeadingWhitespaceInTokenPreamble = ParseBoolean(value, rawName, entry);
                    break;
                case "trimtrailingwhitespace":
                    template.Options.TrimTrailingWhiteSpace = ParseBoolean(value, rawName, entry);
                    break;
                case "trimpreamblebeforenewline":
                    template.Options.TrimPreambleBeforeNewLine = ParseBoolean(value, rawName, entry);
                    break;
                case "outoforder":
                    template.Options.OutOfOrderTokens = ParseBoolean(value, rawName, entry);
                    break;
                case "terminateonnewline":
                    template.Options.TerminateOnNewline = ParseBoolean(value, rawName, entry);
                    break;
                case "ignoremissingproperties":
                    template.Options.IgnoreMissingProperties = ParseBoolean(value, rawName, entry);
                    break;
                case "casesensitive":
                    template.Options.TokenStringComparison = ParseBoolean(value, rawName, entry)
                        ? System.StringComparison.InvariantCulture
                        : System.StringComparison.InvariantCultureIgnoreCase;
                    break;
                case "name":
                    template.Name = value.Trim();
                    break;
                case "hint":
                    template.Hints.Add(new Hint { Text = value.Trim(), Optional = false });
                    break;
                case "hint?":
                    template.Hints.Add(new Hint { Text = value.Trim(), Optional = true });
                    break;
                case "tag":
                    template.Tags.Add(value.Trim());
                    break;
                default:
                    throw new ParsingException($"Unknown front matter option: {rawName}", entry.Location);
            }
        }

        private static bool ParseBoolean(string input, string rawName, FrontMatterEntry entry)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", entry.Location);
            }
            var v = input.Trim().ToLowerInvariant();
            if (v == "true" || v == "yes" || v == "on") return true;
            if (v == "false" || v == "no" || v == "off") return false;
            throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", entry.Location);
        }
    }
}


