using System.Collections.Generic;
using System.Text;

namespace Tokens.Compilation.Definitions;

public class DecoratorDefinition
{
    private readonly StringBuilder name;

    public DecoratorDefinition()
    {
        name = new StringBuilder();
        Args = new List<string>();
    }

    public string Name => name.ToString();

    public IList<string> Args { get; }

    public bool IsNotDecorator { get; set; }

    public void AppendName(string value)
    {
        name.Append(value);
    }
}
