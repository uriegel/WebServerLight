using System.Net.Sockets;
using static System.Console;

namespace WebServerLight;

class RequestSession(Server server, SocketSession socketSession, Stream networkStream, DateTime? startTime)
{
    public string Id { get; } = socketSession.Id + "-" + Interlocked.Increment(ref seedId);

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True: Keep this SocketSession alive, otherwise dispose it</returns>
    public async Task<bool> StartAsync()
    {
        try
        {
            var keepAliveCancellation = new CancellationTokenSource(server.SocketLifetime);
            var msg = await Message.Read(server, networkStream, keepAliveCancellation.Token);
            if (msg == null)
            {
                WriteLine(() => $"{Id} Socket session closed");
                return false;
            }
            // if (!RequestStartTime.HasValue)
            //     RequestStartTime = DateTime.Now;
            //return await ReceiveAsync(read);
            return false;
        }
        catch (OperationCanceledException)
        {
            Error.WriteLine($"{Id} Closing socket session, lifetime exceeded");
            Close(true);
            return false;
        }
        catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
        {
            WriteLine($"{Id} Closing socket session: {e}");
            Close(true);
            return false;
        }
        catch (Exception e) when (e is ObjectDisposedException)
        {
            WriteLine($"{Id} Object disposed");
            Close(true);
            return false;
        }
        catch (Exception e)
        {
            Error.WriteLine($"{Id} An error has occurred while reading socket: {e}");
            Close(true);
            return false;
        }
    }

    public void Close(bool fullClose = false)
    {
        try
        {
            if (fullClose)
            {
                networkStream.Close();
                isClosed = true;
            }
            else
                socketSession.TcpClient.Client.Shutdown(SocketShutdown.Send);
        }
        catch { }
    }

    static int seedId;

    bool isClosed;
}
