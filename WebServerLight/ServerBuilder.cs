using CsTools.Extensions;
using WebServerLight.Routing;
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
    /// <returns></returns>
    public ServerBuilder WebsiteFromResource()
        => this.SideEffect(_ => IsWebsiteFromResource = true);

    public ServerBuilder Get(Func<IRequest, Task<bool>> request)
        => this.SideEffect(_ => GetRequests.Add(request));

    public ServerBuilder Get(Route routes)
        => this.SideEffect(_ => GetRoutes.Add(routes));

    public ServerBuilder JsonPost(Func<IRequest, Task<bool>> request)
        => this.SideEffect(_ => jsonPost = request);

    public ServerBuilder WebSocket(Action<IWebSocket> onWebSocket)
        => this.SideEffect(_ => this.onWebSocket = onWebSocket);

    public ServerBuilder KeepAliveTime(TimeSpan keepAliveTime)
        => this.SideEffect(_ => SocketLifetime = ((int)keepAliveTime.TotalSeconds).SideEffect(t => WriteLine($"KeepAlive time: {t} s")));

    public ServerBuilder AddAllowedOrigin(string origin)
        => this.SideEffect(_ => allowedOrigins.Add(origin));

    public ServerBuilder AccessControlMaxAge(TimeSpan maxAge)
        => this.SideEffect(_ => AccessControlMaxAgeStr = $"{(int)maxAge.TotalSeconds}");

    public ServerBuilder UseRange()
        => this.SideEffect(_ => useRange = true);

    /// <summary>
    /// Aftern configuring the Builder, call this method for creating a Web Server instance.
    /// </summary>
    /// <returns></returns>
    public IServer Build()
        => new Server(this);

    internal IReadOnlyList<string> AllowedOrigins { get => allowedOrigins; }    
    internal int? HttpPort { get; private set; }
    internal int SocketLifetime { get; private set; } = 3 * 60_000;
    internal bool IsWebsiteFromResource { get; private set; }
    internal Func<IRequest, Task<bool>>? jsonPost;
    internal List<Func<IRequest, Task<bool>>> GetRequests { get; } = [];
    internal List<Route> GetRoutes { get; } = [];
    internal Action<IWebSocket>? onWebSocket;
    internal string? AccessControlMaxAgeStr { get; private set; }
    internal bool useRange { get; private set; }

    ServerBuilder() { }

    readonly List<string> allowedOrigins = [];
} 