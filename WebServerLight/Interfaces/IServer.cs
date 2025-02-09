namespace WebServerLight;

/// <summary>
/// Interface for accessing the WebServer
/// </summary>
public interface IServer
{
    /// <summary>
    /// Starts the Web Server
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the Web Server. It can be started again
    /// </summary>
    void Stop();
}