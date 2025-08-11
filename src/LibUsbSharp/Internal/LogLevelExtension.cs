using Microsoft.Extensions.Logging;

namespace LibUsbSharp.Internal;

internal static class LogLevelExtension
{
    public static LibUsbLogLevel ToLibUsbLogLevel(this LogLevel logLevel) =>
        logLevel switch
        {
            // LibUsbLogLevel.Debug is very verbose and is best mapped to .NET LogLevel.Trace
            LogLevel.Trace => LibUsbLogLevel.Debug,
            LogLevel.Debug => LibUsbLogLevel.Info,
            LogLevel.Information => LibUsbLogLevel.Info,
            LogLevel.Warning => LibUsbLogLevel.Warning,
            LogLevel.Error => LibUsbLogLevel.Error,
            LogLevel.Critical => LibUsbLogLevel.Error,
            LogLevel.None => LibUsbLogLevel.None,
        };
}
