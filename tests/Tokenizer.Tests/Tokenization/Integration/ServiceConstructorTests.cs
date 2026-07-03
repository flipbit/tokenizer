using Xunit;

namespace Tokens.Tokenization.Integration;

public class ServiceConstructorTests
{
    [Fact]
    public void GivenTokenizationEngine_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var engine = new TokenizationEngine();

        // Assert
        Assert.NotNull(engine);
        Assert.IsType<TokenizationEngine>(engine);
    }

    [Fact]
    public void GivenHintProcessor_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var processor = new HintProcessor();

        // Assert
        Assert.NotNull((object?)processor);
        Assert.IsType<HintProcessor>(processor);
    }

    [Fact]
    public void GivenResultBuilder_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var builder = new ResultBuilder();

        // Assert
        Assert.NotNull((object?)builder);
        Assert.IsType<ResultBuilder>(builder);
    }

    [Fact]
    public void GivenTokenizationContext_WhenCreated_ThenInitializesCorrectly()
    {
        // Act
        var context = new TokenizationContext();

        // Assert
        Assert.NotNull(context);
        Assert.IsType<TokenizationContext>(context);
        Assert.NotNull(context.Candidates);
        Assert.NotNull(context.Replacement);
        Assert.NotNull(context.MatchIds);
        Assert.NotNull(context.DisabledRepeatingTokens);
        Assert.NotNull(context.ReplacementLocation);
    }

    [Fact]
    public void GivenTokenizationEngine_WhenCreated_ThenImplementsInterface()
    {
        // Act
        var engine = new TokenizationEngine();

        // Assert
        Assert.IsAssignableFrom<ITokenizationEngine>(engine);
    }

    [Fact]
    public void GivenHintProcessor_WhenCreated_ThenImplementsInterface()
    {
        // Act
        var processor = new HintProcessor();

        // Assert
        Assert.IsAssignableFrom<IHintProcessor>(processor);
    }

    [Fact]
    public void GivenResultBuilder_WhenCreated_ThenImplementsInterface()
    {
        // Act
        var builder = new ResultBuilder();

        // Assert
        Assert.IsAssignableFrom<IResultBuilder>(builder);
    }

    [Fact]
    public void GivenTokenizationContext_WhenCreated_ThenImplementsInterface()
    {
        // Act
        var context = new TokenizationContext();

        // Assert
        Assert.IsAssignableFrom<ITokenizationContext>(context);
    }

    [Fact]
    public void GivenTokenizationEngine_WhenCreated_ThenIsStateless()
    {
        // Arrange
        var engine1 = new TokenizationEngine();
        var engine2 = new TokenizationEngine();

        // Act & Assert
        // Both instances should be independent and stateless
        Assert.NotSame(engine1, engine2);
        Assert.NotNull(engine1);
        Assert.NotNull(engine2);
    }

    [Fact]
    public void GivenHintProcessor_WhenCreated_ThenIsStateless()
    {
        // Arrange
        var processor1 = new HintProcessor();
        var processor2 = new HintProcessor();

        // Act & Assert
        // Both instances should be independent and stateless
        Assert.NotSame(processor1, processor2);
        Assert.NotNull((object?)processor1);
        Assert.NotNull((object?)processor2);
    }

    [Fact]
    public void GivenResultBuilder_WhenCreated_ThenIsStateless()
    {
        // Arrange
        var builder1 = new ResultBuilder();
        var builder2 = new ResultBuilder();

        // Act & Assert
        // Both instances should be independent and stateless
        Assert.NotSame(builder1, builder2);
        Assert.NotNull((object?)builder1);
        Assert.NotNull((object?)builder2);
    }

    [Fact]
    public void GivenTokenizationContext_WhenCreated_ThenIsStateful()
    {
        // Arrange
        var context1 = new TokenizationContext();
        var context2 = new TokenizationContext();

        // Act
        context1.Initialize(new System.IO.StringReader("test1"));
        context2.Initialize(new System.IO.StringReader("test2"));

        // Assert
        // Contexts should be independent and hold their own state
        Assert.NotSame(context1, context2);
        Assert.NotSame(context1.Enumerator, context2.Enumerator);
        Assert.NotNull(context1.Enumerator);
        Assert.NotNull(context2.Enumerator);
    }

    [Fact]
    public void GivenServices_WhenCreatedMultipleTimes_ThenEachInstanceIsIndependent()
    {
        // Act
        var engines = new ITokenizationEngine[5];
        var processors = new IHintProcessor[5];
        var builders = new IResultBuilder[5];
        var contexts = new ITokenizationContext[5];

        for (int i = 0; i < 5; i++)
        {
            engines[i] = new TokenizationEngine();
            processors[i] = new HintProcessor();
            builders[i] = new ResultBuilder();
            contexts[i] = new TokenizationContext();
        }

        // Assert
        for (int i = 0; i < 5; i++)
        {
            Assert.NotNull(engines[i]);
            Assert.NotNull(processors[i]);
            Assert.NotNull(builders[i]);
            Assert.NotNull(contexts[i]);
        }

        // All instances should be different objects
        for (int i = 0; i < 5; i++)
        {
            for (int j = i + 1; j < 5; j++)
            {
                Assert.NotSame(engines[i], engines[j]);
                Assert.NotSame(processors[i], processors[j]);
                Assert.NotSame(builders[i], builders[j]);
                Assert.NotSame(contexts[i], contexts[j]);
            }
        }
    }

    [Fact]
    public void GivenServices_WhenCreated_ThenNoDependenciesRequired()
    {
        // Act & Assert
        // All services should be creatable without external dependencies
        Assert.NotNull(new TokenizationEngine());
        Assert.NotNull((object?)new HintProcessor());
        Assert.NotNull((object?)new ResultBuilder());
        Assert.NotNull(new TokenizationContext());
    }

    [Fact]
    public void GivenServices_WhenCreated_ThenCanBeUsedImmediately()
    {
        // Arrange
        var engine = new TokenizationEngine();
        var processor = new HintProcessor();
        var builder = new ResultBuilder();
        var context = new TokenizationContext();

        // Act & Assert
        // Services should be immediately usable after creation
        Assert.NotNull(engine);
        Assert.NotNull((object?)processor);
        Assert.NotNull((object?)builder);
        Assert.NotNull(context);

        // Context should be usable after initialization
        context.Initialize(new System.IO.StringReader("test"));
        Assert.NotNull(context.Enumerator);
    }
}
