using System.Security.Cryptography.X509Certificates;
using CsTools.Extensions;
using WebServerLight.Routing;

using static System.Console;

namespace WebServerLight;

/// <summary>
/// Builder pattern for fluently creating the Web Server
/// </summary>
public class WebServer
{
    /// <summary>
    /// Creates a WebServer builder
    /// </summary>
    /// <returns></returns>
    public static WebServer New() => new();

    /// <summary>
    /// Enabling (insecure) HTTP support and configuring port
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public WebServer Http(int port = 80)
        => this.SideEffect(_ => HttpPort = port.SideEffect(p => WriteLine($"Using HTTP port {p}")));

    /// <summary>
    /// Enabling secure HTTPS support and configuring port
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public WebServer Https(int port = 443)
        => this.SideEffect(_ => HttpsPort = port.SideEffect(p => WriteLine($"Using HTTPS port {p}")));

    /// <summary>
    /// Certificate necessary for HTTPS. Alternatively you could call UseLetsEncrypt() and let LetsEncrypt do all the work
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public WebServer HttpsCertificate(X509Certificate2 certificate)
        => this.SideEffect(_ => Certificate = certificate);

    /// <summary>
    /// Automatically uses Let's Encrypt certificate. The .NET tool "LetsEncryptCert" is necessary and works with ths Web Server.
    /// </summary>
    /// <returns></returns>
    public WebServer UseLetsEncrypt()
        => this.SideEffect(_ => LetsEncrypt = true);

    /// <summary>
    /// Host website, the files are included as .NET resource in the executing Assembly
    /// </summary>
    /// <returns></returns>
    public WebServer WebsiteFromResource()
        => this.SideEffect(_ => IsWebsiteFromResource = true);

    public WebServer Route(Route route)
        => this.SideEffect(_ => Routes.Add(route));

    public WebServer WebSocket(Action<IWebSocket> onWebSocket)
        => this.SideEffect(_ => this.onWebSocket = onWebSocket);

    public WebServer KeepAliveTime(TimeSpan keepAliveTime)
        => this.SideEffect(_ => SocketLifetime = ((int)keepAliveTime.TotalSeconds).SideEffect(t => WriteLine($"KeepAlive time: {t} s")));

    public WebServer AddAllowedOrigin(string origin)
        => this.SideEffect(_ => allowedOrigins.Add(origin));

    public WebServer AccessControlMaxAge(TimeSpan maxAge)
        => this.SideEffect(_ => AccessControlMaxAgeStr = $"{(int)maxAge.TotalSeconds}");

    public WebServer UseRange()
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
    
    WebServer() { }

    readonly List<string> allowedOrigins = [];
} 