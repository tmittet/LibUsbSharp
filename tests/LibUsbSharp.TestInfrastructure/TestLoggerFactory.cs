namespace LibUsbSharp.TestInfrastructure;

public sealed class TestLoggerFactory : ILoggerFactory
{
    private readonly TestLoggerProvider _provider;

    public TestLoggerFactory(ITestOutputHelper output, LogLevel minLevel = LogLevel.Trace)
    {
        _provider = new TestLoggerProvider(output, minLevel);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotSupportedException("Adding providers is not supported in this factory.");
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _provider.CreateLogger(categoryName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
