using System.Globalization;
using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Binds front matter AST nodes onto <see cref="TemplateDefinition"/> and its <see cref="TokenizerOptions"/>.
/// </summary>
internal static class FrontMatterBinder
{
    public static void Bind(TemplateDefinition template, FrontMatterBlock? frontMatter)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (frontMatter == null) return;
        // Ensure options are available for binding
        template.Options ??= new TokenizerOptions();

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
            Location = dir.Location.Clone(),
        };
        token.AppendName(dir.TokenName);
        if (dir.Value is { Length: > 0 } dirValue)
        {
            token.AppendValue(dirValue);
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
                template.Options = template.Options with
                {
                    TrimLeadingWhitespaceInTokenPreamble = ParseBoolean(value, rawName, entry),
                };
                break;
            case "trimtrailingwhitespace":
                template.Options = template.Options with
                {
                    TrimTrailingWhiteSpace = ParseBoolean(value, rawName, entry),
                };
                break;
            case "trimpreamblebeforenewline":
                template.Options = template.Options with
                {
                    TrimPreambleBeforeNewLine = ParseBoolean(value, rawName, entry),
                };
                break;
            case "outoforder":
                template.Options = template.Options with
                {
                    OutOfOrderTokens = ParseBoolean(value, rawName, entry),
                };
                break;
            case "terminateonnewline":
                template.Options = template.Options with
                {
                    TerminateOnNewLine = ParseBoolean(value, rawName, entry),
                };
                break;
            case "ignoremissingproperties":
                template.Options = template.Options with
                {
                    IgnoreMissingProperties = ParseBoolean(value, rawName, entry),
                };
                break;
            case "casesensitive":
                template.Options = template.Options with
                {
                    TokenStringComparison = ParseBoolean(value, rawName, entry)
                        ? System.StringComparison.InvariantCulture
                        : System.StringComparison.InvariantCultureIgnoreCase,
                };
                break;
            case "name":
                template.Name = value.Trim();
                break;
            case "hint":
                template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: false));
                break;
            case "hint?":
                template.Hints.Add(new Hint(Text: entry.RawValue.Trim(), Optional: true));
                break;
            case "tag":
                template.Tags.Add(value.Trim());
                break;
            case "culture":
                try
                {
                    var cultureName = value.Trim();
#if NET6_0_OR_GREATER
                    // predefinedOnly: true ensures CultureNotFoundException is thrown for invalid names
                    // (without it, .NET 6+ ICU silently creates a custom culture)
                    var culture = CultureInfo.GetCultureInfo(cultureName, predefinedOnly: true);
#else
                    var culture = CultureInfo.GetCultureInfo(cultureName);
#endif
                    template.Options = template.Options with { Culture = culture };
                }
                catch (CultureNotFoundException)
                {
                    throw new ParsingException(
                        $"Invalid culture name: {value.Trim()}", entry.Location);
                }
                break;
            case "defaultoffset":
                var offsetStr = value.Trim();
                // Strip leading '+' because TimeSpan.TryParse does not accept it
                var parseStr = offsetStr.Length > 0 && offsetStr[0] == '+'
                    ? offsetStr.Substring(1)
                    : offsetStr;
                if (!TimeSpan.TryParse(parseStr, CultureInfo.InvariantCulture, out var offset))
                {
                    throw new ParsingException(
                        $"Invalid offset format: {offsetStr}. Expected format: +HH:mm or -HH:mm", entry.Location);
                }
                template.Options = template.Options with { DefaultOffset = offset };
                break;
            case "defaulttimezone":
                template.Options = template.Options with
                {
                    DefaultTimezone = value.Trim(),
                };
                break;
            default:
                throw new ParsingException($"Unknown front matter option: {rawName}", entry.Location);
        }
    }

    internal static bool ParseBoolean(string input, string rawName, FrontMatterEntry entry)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", entry.Location);
        }
        var v = input.Trim();
        if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "no", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "off", StringComparison.OrdinalIgnoreCase)) return false;
        throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", entry.Location);
    }
}


