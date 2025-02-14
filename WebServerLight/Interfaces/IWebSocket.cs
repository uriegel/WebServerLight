namespace WebServerLight;

public interface IWebSocket
{
    string Url { get; }
    Task SendString(string payload);
    Task SendJson<T>(T payload);
    void Close();
}