using System.Security.Cryptography.X509Certificates;
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

    public ServerBuilder Https(int port = 443)
        => this.SideEffect(_ => HttpsPort = port.SideEffect(p => WriteLine($"Using HTTPS port {p}")));

    /// <summary>
    /// Certificate necessary for HTTPS. Alternatively you could call UseLetsEncrypt() and let LetsEncrypt do all the work
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public ServerBuilder HttpsCertificate(X509Certificate2 certificate)
        => this.SideEffect(_ => Certificate = certificate);

    /// <summary>
    /// Automatically uses Let's Encrypt certificate. The .NET tool "LetsEncryptCert" is necessary and works with ths Web Server.
    /// </summary>
    /// <returns></returns>
    public ServerBuilder UseLetsEncrypt()
        => this.SideEffect(_ => LetsEncrypt = true);

    /// <summary>
    /// Host website, the files are included as .NET resource. The files are included in the executing Assembly
    /// </summary>
    /// <returns></returns>
    public ServerBuilder WebsiteFromResource()
        => this.SideEffect(_ => IsWebsiteFromResource = true);

    public ServerBuilder Route(Route route)
        => this.SideEffect(_ => Routes.Add(route));

    public ServerBuilder WebSocket(Action<IWebSocket> onWebSocket)
        => this.SideEffect(_ => this.onWebSocket = onWebSocket);

    public ServerBuilder KeepAliveTime(TimeSpan keepAliveTime)
        => this.SideEffect(_ => SocketLifetime = ((int)keepAliveTime.TotalSeconds).SideEffect(t => WriteLine($"KeepAlive time: {t} s")));

    public ServerBuilder AddAllowedOrigin(string origin)
        => this.SideEffect(_ => allowedOrigins.Add(origin));

    public ServerBuilder AccessControlMaxAge(TimeSpan maxAge)
        => this.SideEffect(_ => AccessControlMaxAgeStr = $"{(int)maxAge.TotalSeconds}");

    public ServerBuilder UseRange()
        => this.SideEffect(_ => UseRangeValue = true);

    /// <summary>
    /// After configuring the Builder, call this method for creating a Web Server instance.
    /// </summary>
    /// <returns></returns>
    public IServer Build()
        => new Server(this);

    internal IReadOnlyList<string> AllowedOrigins { get => allowedOrigins; }    
    internal int? HttpPort { get; private set; }
    internal int? HttpsPort { get; private set; }
    internal int SocketLifetime { get; private set; } = 3 * 60_000;
    internal bool IsWebsiteFromResource { get; private set; }
    internal List<Route> Routes { get; } = [];
    internal Action<IWebSocket>? onWebSocket;
    internal string? AccessControlMaxAgeStr { get; private set; }
    internal bool UseRangeValue { get; private set; }
    internal bool LetsEncrypt { get; private set; }
    internal X509Certificate2? Certificate { get; private set; }
    internal bool AllowRenegotiation { get; private set; }
    
    ServerBuilder() { }

    readonly List<string> allowedOrigins = [];
} 