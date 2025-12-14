namespace UdpConnection.Logging;

public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
}

public interface ILogger
{
    bool IsEnabled(LogLevel level);
    void Log(LogLevel level, string message);

    void LogD(string message) => Log(LogLevel.Debug, message);
    void LogI(string message) => Log(LogLevel.Information, message);
    void LogW(string message) => Log(LogLevel.Warning, message);
    void LogE(string message) => Log(LogLevel.Error, message);
}
