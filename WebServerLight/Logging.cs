namespace WebServerLight;

public enum LogLevel
{
    Trace,
    Info,
    Error,
    Disabled
}
static class Logging
{
    public static void WriteLine(string text, LogLevel logLevel = LogLevel.Trace)
    {
        if (logLevel >= LogLevel)
            Console.WriteLine(text);
    }

    public static LogLevel LogLevel { get; set; } = LogLevel.Info;
}

