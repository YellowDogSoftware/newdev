using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace YellowDogSoftware.NewDev;

public class BasicConsoleFormatterOptions : ConsoleFormatterOptions
{
}

public class BasicConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "basic";

    public BasicConsoleFormatter(IOptions<BasicConsoleFormatterOptions> options)
        : base(FormatterName)
    {
        //
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        textWriter.Write(logEntry.Formatter(logEntry.State, logEntry.Exception));

        if ( logEntry.Exception != null )
        {
            textWriter.Write($": {logEntry.Exception.Message}");
        }

        textWriter.WriteLine();
    }
}