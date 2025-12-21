using UdpConnection.Logging;

namespace UdpConnection.SimpleCUI.Controller;

public class ConsoleLogger : ILogger
{
    private readonly LogLevel _minLevel;
    private readonly object _lock = new();

    public ConsoleLogger(LogLevel minLevel = LogLevel.Information)
    {
        _minLevel = minLevel;
    }

    public bool IsEnabled(LogLevel level) => level >= _minLevel;

    public void Log(LogLevel level, string message)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        lock (_lock)
        {
            Console.Error.WriteLine($"[{timestamp}] {message}");
        }
    }
}
