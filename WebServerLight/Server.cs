using System.Net;
using System.Net.Sockets;
using CsTools;
using WebServerLight.Routing;
using WebServerLight.Sessions;
using static System.Console;

namespace WebServerLight;

class Server(ServerBuilder server) : IServer
{
    public bool IsStarted { get; private set; }

    public ServerBuilder Configuration { get => server; }

    public int SocketLifetime { get => server.SocketLifetime; }

    public Route Routes { get; private set; } = new Route([]);

    public void Start()
    {
        WriteLine("Starting...");
        ServicePointManager.DefaultConnectionLimit = 1000;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        var routes = new List<Route>
        {
            new MethodRoute(Method.Options, [ new RequestRoute(Requests.ServeOptions) ])
        };

        if (Configuration.onWebSocket != null)
            routes.Add(new UpdateRoute());

        var getRoute = new List<Route>();
        foreach (var route in Configuration.Routes)
            getRoute.Add(route);
        if (Configuration.IsWebsiteFromResource)
            getRoute.Add(new RequestRoute(Requests.ServeResourceWebsite));
        if (getRoute.Count > 0)
            routes.Add(new MethodRoute(Method.Get, getRoute));

        var routeList = new List<Route>();
        foreach (var route in Configuration.Routes)
            routeList.Add(route);
        if (routeList.Count > 0)
            routes.AddRange(routeList);

        if (Configuration.LetsEncrypt)
            routes.Add(new LetsEncryptRoute());

        routes.Add(new RequestRoute(Requests.Send404));
        Routes = new Route(routes);

        if (listener != null)
        {
            WriteLine("Starting HTTP listener...");
            listener.Start();
            WriteLine("HTTP listener started");
        }
        if (tlsListener != null)
        {
            WriteLine("Starting HTTPS listener...");
            tlsListener.Start();
            WriteLine("HTTPS listener started");
        }
        IsStarted = true;
        if (listener != null)
            StartConnecting(listener, false);
        if (tlsListener != null)
            StartConnecting(tlsListener, true);

        WriteLine("Started");
    }

    public void Stop()
    {
        WriteLine("Stopping...");
        if (listener != null)
        {
            WriteLine("Stopping HTTP listener...");
            listener.Stop();
            WriteLine("HTTP listener stopped");
        }

        if (tlsListener != null)
        {
            WriteLine("Stopping HTTPS listener...");
            tlsListener.Stop();
            WriteLine("HTTPS listener stopped");
        }
        WriteLine("Stopped");
    }

    void StartConnecting(TcpListener listener, bool isSecured)
    {
        if (!IsStarted)
            return;

        new Thread(() =>
        {
            try
            {
                while (IsStarted)
                {
                    var client = listener.AcceptTcpClient();
                    OnConnected(client, isSecured);
                }
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.Interrupted && !IsStarted)
            {
            }
            catch (Exception e)
            {
                WriteLine($"Error occurred in connecting thread: {e}");
            }
        })
        {
            IsBackground = true
        }.Start();
    }

    void OnConnected(TcpClient tcpClient, bool isSecured)
    {
        try
        {
            if (!IsStarted)
                return;
            SocketSession.StartReceiving(this, tcpClient, isSecured);
        }
        catch (SocketException se) when (se.NativeErrorCode == 10054)
        {
        }
        catch (ObjectDisposedException)
        {
            // Stop() aufgerufen
            return;
        }
        catch (Exception e)
        {
            if (!IsStarted)
                return;
            WriteLine($"Error in OnConnected occurred: {e}");
        }
    }

    readonly TcpListener? listener = server.HttpPort.HasValue ? DualModeTcpListener.Create(server.HttpPort.Value).Listener : null;
    readonly TcpListener? tlsListener = server.HttpsPort.HasValue ? DualModeTcpListener.Create(server.HttpsPort.Value).Listener : null;
}
 