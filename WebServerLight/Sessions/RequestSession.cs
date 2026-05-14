using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CsTools.Extensions;

using WebServerLight.Routing;
using static WebServerLight.Logging;

namespace WebServerLight.Sessions;

class RequestSession(Server server, SocketSession socketSession, Stream networkStream, DateTime? startTime, bool isSecured)
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
            var msg = await Message.Read(server, networkStream, isSecured, keepAliveCancellation);
            stopwatch.Start();
            if (msg != null)
            {
                WriteLine($"{Id} {msg.Method} {msg.Url}");
                return await Receive(msg);
            }
            else
            {
                WriteLine($"{Id} Socket session closed");
                closed = true;
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            WriteLine($"{Id} Closing socket session, lifetime exceeded", LogLevel.Error);
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
            WriteLine($"{Id} An error has occurred while reading socket: {e}", LogLevel.Error);
            Close(true);
            return false;
        }
        finally
        {
            var elapsed = stopwatch?.Elapsed;
            stopwatch?.Stop();
            // TODO WriteLine($"{Id} Answer: {RemoteEndPoint} \"{Headers.Method} {Headers.Url.CutAt('?')} {Headers.Http}\" Status: {responseHeaders.Status} Size: {responseHeaders.ContentLength} Duration: {elapsed}");
            if (!closed)
                WriteLine($"{Id} Answer: {socketSession.TcpClient.Client.RemoteEndPoint as IPEndPoint} Duration: {elapsed}");
        }
    }

    public void Close(bool fullClose = false)
    {
        try
        {
            if (fullClose)
                networkStream.Close();
            else
                socketSession.TcpClient.Client.Shutdown(SocketShutdown.Send);
        }
        catch { }
    }

    async Task<bool> Receive(Message msg)
    {
        try
        {
            // TODO set range from Routing
            msg.UseRange = server.Configuration.UseRangeValue;
            return await server.Routes.Probe(msg, null) switch
            {
                RouteResult.Keepalive => true,
                RouteResult.Detach => false,
                RouteResult.Next => await true.SideEffectAsync(async _ => await Requests.Send404(msg)),
                _ => false.SideEffect(_ => Close())
            };
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.TimedOut)
            {
                WriteLine($"{Id} Socket session closed, Timeout has occurred", LogLevel.Error);
                Close(true);
                return false;
            }
            return true;
        }
        catch (ConnectionClosedException)
        {
            WriteLine($"{Id} Socket session closed via exception", LogLevel.Error);
            Close(true);
            return false;
        }
        catch (ObjectDisposedException oe)
        {
            WriteLine($"{Id} Socket session closed, an error has occurred: {oe}", LogLevel.Error);
            Close(true);
            return false;
        }
        catch (IOException ioe) when (ioe.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionReset)
        {
            WriteLine($"{Id} Socket session reset by peer", LogLevel.Error);
            Close();
            return false;
        }
        catch (IOException ioe) when (ioe.InnerException is SocketException se && se.SocketErrorCode == SocketError.Shutdown)
        {
            WriteLine($"{Id} Socket session shutdown by peer", LogLevel.Error);
            Close(true);
            return false;
        }
        catch (IOException ioe)
        {
            WriteLine($"{Id} Socket session closed: {ioe}", LogLevel.Error);
            Close(true);
            return false;
        }
        catch (ConnectionResetException)
        {
            Close(true);
            return false;
        }
        catch (Exception e)
        {
            // TODO Send 500 Server Error
            WriteLine($"{Id} Socket session closed, an error has occurred while receiving: {e}", LogLevel.Error);
            Close(true);
            return false;
        }
    }

    static int seedId;
    readonly Stopwatch stopwatch = new();
    readonly CancellationToken keepAliveCancellation = new CancellationTokenSource(server.SocketLifetime).Token;
    bool closed;
}

// TODO if Modified
// TODO Json serializing and File download with Content-Encoding chunked
// TODO HTTPS
// TODO Routing modules (OnHost, OnSecure, OnGet, OnPost, OnJson, ...)

// TODO PostJson:
// else if (msg.Method == Method.Post
//         && string.Compare(msg.RequestHeaders.GetValue("Content-Type"), MimeTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase) == 0
//         && await CheckPostJsonRequest(msg))
