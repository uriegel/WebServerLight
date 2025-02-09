using System.Net;
using System.Net.Sockets;
using CsTools;
using static System.Console;

namespace WebServerLight;

class Server(ServerBuilder server) : IServer
{
    public bool IsStarted { get; private set; }

    public ServerBuilder Configuation { get => server; }

    public int SocketLifetime { get => server.SocketLifetime; }

    public void Start()
    {
        WriteLine("Starting...");
        ServicePointManager.DefaultConnectionLimit = 1000;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
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
        if (tlsRedirectorListener != null)
        {
            WriteLine("Starting HTTP redirection listener...");
            tlsRedirectorListener.Start();
            WriteLine("HTTPS redirection listener started");
        }
        IsStarted = true;
        if (listener != null)
            StartConnecting(listener, false);
        if (tlsListener != null)
            StartConnecting(tlsListener, true);
        if (tlsRedirectorListener != null)
            StartConnecting(tlsRedirectorListener, true);

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
        if (tlsRedirectorListener != null)
        {
            WriteLine("Stopping HTTPS redirection listener...");
            tlsRedirectorListener.Stop();
            WriteLine("HTTPS redirection listener stopped");
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
	readonly TcpListener? tlsListener;
	readonly TcpListener? tlsRedirectorListener;
}