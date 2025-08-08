using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace LibUsbSharp.Tests.TestLogger;

public sealed class TestLoggerProvider(ITestOutputHelper _output) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, _output);
    }

    public void Dispose() { }
}

