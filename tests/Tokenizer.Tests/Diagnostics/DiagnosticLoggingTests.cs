using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ScalarValue = Serilog.Events.ScalarValue;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Diagnostics;

public class DiagnosticLoggingTests : TokenizerTestBase
{
    public DiagnosticLoggingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenDiagnosticTokenizer_WhenTokenizing_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var template = tokenizer.Compile("Name: { Name }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Verdict);
    }

    [Fact]
    public void GivenDiagnosticTokenizer_WhenTokenizingWithFailure_ThenSummaryHasIssues()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Tokens.SelectMany(t => t.Issues));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenIssueHasStableTKCode()
    {
        // Arrange
        var tokenizer = CreateDiagnosticTokenizer();

        // Act
        var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
        var result = tokenizer.Tokenize(template, "Name: John");

        // Assert
        var issues = result.Diagnostics!.Tokens.SelectMany(t => t.Issues).ToList();
        Assert.All(issues, issue =>
        {
            Assert.NotNull(issue.Code);
            Assert.Matches(@"^TK\d{3}$", issue.Code);
        });
        Assert.Contains(issues, i => string.Equals(i.Code, "TK001", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenDiagnosticsEnabled_WhenTokenMissed_ThenWarningLoggedWithIssueCode()
    {
        // Arrange
        var logEvents = new List<LogEvent>();
        var sink = new ListSink(logEvents);
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .WriteTo.TestOutput(Output)
            .CreateLogger();
        using var loggerFactory = new SerilogLoggerFactory(serilogLogger);
        var tokenizer = new Tokenizer(new TokenizerOptions { EnableDiagnostics = true }, loggerFactory);

        // Act
        var template = tokenizer.Compile("Name: { Name }\nAge: { Age }").Template;
        tokenizer.Tokenize(template, "Name: John");

        // Assert
        var warnings = logEvents.Where(e => e.Level == LogEventLevel.Warning).ToList();
        Assert.NotEmpty(warnings);
        var issueWarning = warnings.First(e => e.MessageTemplate.Text.Contains("{IssueCode}", StringComparison.Ordinal));
        var issueCode = (ScalarValue)issueWarning.Properties["IssueCode"];
        var tokenName = (ScalarValue)issueWarning.Properties["TokenName"];
        Assert.Equal("TK001", issueCode.Value);
        Assert.Equal("Age", tokenName.Value);
    }

    private sealed class ListSink : Serilog.Core.ILogEventSink
    {
        private readonly List<LogEvent> _events;

        public ListSink(List<LogEvent> events)
        {
            _events = events;
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }
}
