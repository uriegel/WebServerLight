using CsTools.Extensions;

using static System.Console;

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

    public ServerBuilder Http(int port = 80)
        => this.SideEffect(_ => HttpPort = port.SideEffect(p => WriteLine($"Using HTTP port {p}")));

    /// <summary>
    /// Host website, the files are included as .NET resource. The files are included in the executing Assembly
    /// </summary>
    /// <param name="resourceBasePath"></param>
    /// <returns></returns>
    public ServerBuilder WebsiteFromResource(string resourceBasePath)
        => this.SideEffect(_ => ResourceBasePath = resourceBasePath);

    public ServerBuilder KeepAliveTime(TimeSpan keepAliveTime)
        => this.SideEffect(_ => SocketLifetime = ((int)keepAliveTime.TotalSeconds).SideEffect(t => WriteLine($"KeepAlive time: {t} s")));

    /// <summary>
    /// Aftern configuring the Builder, call this method for creating a Web Server instance.
    /// </summary>
    /// <returns></returns>
    public IServer Build()
        => new Server(this);

    internal int? HttpPort { get; private set; }
    internal int SocketLifetime { get; private set; } = 3 * 60_000;
    internal string? ResourceBasePath { get; private set; }
    private ServerBuilder() { }
} 