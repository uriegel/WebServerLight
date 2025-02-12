using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CsTools.Extensions;

using static System.Console;

namespace WebServerLight;

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
            var msg = await Message.Read(networkStream, keepAliveCancellation);
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
            if (msg.Method == Method.Post
                    && string.Compare(msg.RequestHeaders.GetValue("Content-Type"), "application/json", StringComparison.OrdinalIgnoreCase) == 0
                    && await CheckPostJsonRequest(msg))
                return true;
            else if (server.Configuation.ResourceBasePath != null && await CheckResourceWebsite(msg))
                return true;
            else
                await Send404(msg);
            return true;
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
    async Task<bool> CheckResourceWebsite(Message msg)
    {
        var url = msg.Url.SubstringUntil('?');
        url = url != "/" ? url : "/index.html";
        var res = Resources.Get(url);
        if (res != null)
        {
            msg.AddResponseHeader("Connection", "Keep-Alive");
            msg.AddResponseHeader("Content-Length", $"{res.Length}");
            msg.AddResponseHeader("Content-Type", url?.GetFileExtension()?.ToMimeType() ?? "text/html");
            await SendStream(msg, res);
            return true;
        }
        else
            return false;
    }

    async Task<bool> CheckPostJsonRequest(Message msg)
    {
        var url = msg.Url.SubstringUntil('?');
        var length = msg.RequestHeaders.GetValue("Content-Length")?.ParseInt();
        if (length.HasValue && server.Configuation.jsonPost != null)
        {
            var request = new JsonRequest(url, msg.Payload, SendData, keepAliveCancellation);
            return await server.Configuation.jsonPost(request);
        }
        else
            return false;

        async Task SendData(Stream payload)
        {
            msg.AddResponseHeader("Connection", "Keep-Alive");
            msg.AddResponseHeader("Content-Length", $"{payload.Length}");
            msg.AddResponseHeader("Content-Type", "application/json");
            await SendStream(msg, payload);
        }
    }

    async Task SendStream(Message msg, Stream stream)
    {
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", msg.ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
        await stream.CopyToAsync(networkStream);
        await stream.FlushAsync();
    }

    async Task Send404(Message msg)
    {
        var body = "I can't find what you're looking for...";
        msg.AddResponseHeader("Connection", "Keep-Alive");
        msg.AddResponseHeader("Content-Length", $"{body.Length}");
        msg.AddResponseHeader("Content-Type", "text/html");
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 404 Not Found\r\n{string.Join("\r\n", msg.ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n{body}"));
    }

    static int seedId;
    readonly Stopwatch stopwatch = new();

    bool isClosed;
    readonly CancellationToken keepAliveCancellation = new CancellationTokenSource(server.SocketLifetime).Token;

}
