using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

using static System.Console;

namespace WebServerLight.Sessions;

class SocketSession(Server server, TcpClient tcpClient, bool isSecured)
{
    public DateTime ConnectTime { get; } = DateTime.Now;
    public int Id { get; } = Interlocked.Increment(ref seedId);

    public TcpClient TcpClient { get => tcpClient; }

    public static async void StartReceiving(Server server, TcpClient tcpClient, bool isSecured)
    {
        var session = new SocketSession(server, tcpClient, isSecured);
        await session.Receive();
    }

    public async Task Receive()
    {
        try
        {
            while (true)
            {
                DateTime? startTime = null;
                if (networkStream == null)
                {
                    networkStream = isSecured ? await TcpClient.GetTlsNetworkStreamAsync(Id, server.Configuration) : TcpClient.GetStream();
                    startTime = ConnectTime;
                }

                if (shutdown)
                    break;
                var session = new RequestSession(server, this, networkStream!, startTime, isSecured);
                if (!await session.StartAsync())
                    break;
            }
        }
        catch (AuthenticationException ae)
        {
            Error.WriteLine($"{Id}- An authentication error has occurred while reading socket, session: {tcpClient.Client.RemoteEndPoint as IPEndPoint}, error: {ae}");
        }
        catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
        {
            Error.WriteLine(() => $"{Id}- Closing socket session, reason: {e}");
            Close();
        }
        catch (Exception e) when (e is ObjectDisposedException)
        {
            Error.WriteLine($"{Id} Object disposed");
            Close();
        }
        catch (Exception e)
        {
            Error.WriteLine($"{Id} An error has occurred while reading socket, error: {e}");
        }
    }

    public void Close() => TcpClient.Close();

    public static void Shutdown() => shutdown = true;

    static int seedId;
    static bool shutdown;
    Stream? networkStream;
}