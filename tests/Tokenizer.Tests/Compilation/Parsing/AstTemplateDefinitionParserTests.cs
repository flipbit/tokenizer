using Xunit.Abstractions;

namespace Tokens.Compilation.Parsing;

public class AstTemplateDefinitionParserTests(ITestOutputHelper testOutputHelper) : BaseTemplateDefinitionParserTests(testOutputHelper)
{
    protected override ITemplateDefinitionParser Parser { get; } = new AstTemplateDefinitionParser();
}
