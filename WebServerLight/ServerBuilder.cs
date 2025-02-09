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
    private ServerBuilder() { }
} 