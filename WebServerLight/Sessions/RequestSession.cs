using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using WebServerLight;
using WebServerLight.Sessions;

using static System.Console;

namespace WebServerLightSessions;

class RequestSession(Server server, SocketSession socketSession, Stream networkStream, DateTime? startTime)
{
    public DateTime? StartTime { get; } = startTime ?? DateTime.Now;
    public string Id
    {
        get => _ID ??= _ID = socketSession.Id + "-" + Interlocked.Increment(ref seedId);
    }
    string? _ID;

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True: Keep this SocketSession alive, otherwise dispose it</returns>
    public async Task<bool> StartAsync()
    {
        try
        {
            var msg = await Message.Read(server, networkStream, keepAliveCancellation);
            stopwatch.Start();
            if (msg != null)
            {
                WriteLine($"{Id} {msg.Method} {msg.Url}");
                return await Receive(msg);
            }
            else
            {
                WriteLine($"{Id} Socket session closed");
                return false;
            }
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
        finally
        {
            var elapsed = stopwatch?.Elapsed;
            stopwatch?.Stop();
            // TODO WriteLine($"{Id} Answer: {RemoteEndPoint} \"{Headers.Method} {Headers.Url.CutAt('?')} {Headers.Http}\" Status: {responseHeaders.Status} Size: {responseHeaders.ContentLength} Duration: {elapsed}");
            WriteLine($"{Id} Answer: {socketSession.TcpClient.Client.RemoteEndPoint as IPEndPoint} Duration: {elapsed}");
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

    async Task<bool> Receive(Message msg)
    {
        try
        {
            return await server.Routes.Probe(msg);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.TimedOut)
            {
                Error.WriteLine($"{Id} Socket session closed, Timeout has occurred");
                Close(true);
                return false;
            }
            return true;
        }
        catch (ConnectionClosedException)
        {
            Error.WriteLine($"{Id} Socket session closed via exception");
            Close(true);
            return false;
        }
        catch (ObjectDisposedException oe)
        {
            Error.WriteLine($"{Id} Socket session closed, an error has occurred: {oe}");
            Close(true);
            return false;
        }
        catch (IOException ioe)
        {
            Error.WriteLine($"{Id} Socket session closed: {ioe}");
            Close(true);
            return false;
        }
        catch (Exception e)
        {
            Error.WriteLine($"{Id} Socket session closed, an error has occurred while receiving: {e}");
            Close(true);
            return false;
        }
    }

    static int seedId;
    readonly Stopwatch stopwatch = new();
    readonly CancellationToken keepAliveCancellation = new CancellationTokenSource(server.SocketLifetime).Token;
    bool isClosed;
}

// TODO WebSockets
// TODO if Modified
// TODO Json serializing and File download with Content-Encoding chunked
// TODO HTTPS
// TODO Routing modules (OnHost, OnSecure, OnGet, OnPost, OnJson, ...)

// TODO PostJson:
// else if (msg.Method == Method.Post
//         && string.Compare(msg.RequestHeaders.GetValue("Content-Type"), MimeTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase) == 0
//         && await CheckPostJsonRequest(msg))
