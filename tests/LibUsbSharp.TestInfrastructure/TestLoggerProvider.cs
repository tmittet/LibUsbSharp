namespace LibUsbSharp.TestInfrastructure;

public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    private readonly LogLevel _minLevel;

    public TestLoggerProvider(ITestOutputHelper output, LogLevel minLevel = LogLevel.Trace)
    {
        _output = output;
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, _output, _minLevel);
    }

    public void Dispose() { }
}
