namespace WebServerLight;

/// <summary>
/// Builder pattern for fluently creating the Web Server
/// </summary>
public class ServerBuilder
{
    /// <summary>
    /// Creates a ServerBuilder
    /// </summary>
    /// <returns></returns>
    public static ServerBuilder New() => new();

    /// <summary>
    /// Aftern configuring the Builder, call this method for creating a Web Server instance.
    /// </summary>
    /// <returns></returns>
    public IServer Build()
    {
        return new Server();
    }

    private ServerBuilder() { }
} 