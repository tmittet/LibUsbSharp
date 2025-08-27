namespace LibUsbSharp.TestInfrastructure;

public class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ITestOutputHelper _output;
    private readonly LogLevel _minLevel;

    public TestLogger(string categoryName, ITestOutputHelper output, LogLevel minLevel)
    {
        _categoryName = categoryName;
        _output = output;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => default;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var outputMessage = $"[{logLevel}] {_categoryName}: {message}";

        if (exception != null)
            outputMessage += Environment.NewLine + exception;

        _output.WriteLine(outputMessage);
    }
}
