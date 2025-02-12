using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
            if (msg.Method == Method.Options && await ServeOptions(msg))
                return true;
            else if (msg.Method == Method.Post
                    && string.Compare(msg.RequestHeaders.GetValue("Content-Type"), MimeTypes.ApplicationJson, StringComparison.OrdinalIgnoreCase) == 0
                    && await CheckPostJsonRequest(msg))
                return true;
            else if (msg.Method == Method.Get && await ServeGet(msg))
                return true;
            else if (server.Configuration.IsWebsiteFromResource && await CheckResourceWebsite(msg))
                return true;
            else
                await msg.Send404();
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
            await msg.SendStream(res, url?.GetFileExtension()?.ToMimeType() ?? MimeTypes.TextHtml, (int)res.Length);
            return true;
        }
        else
            return false;
    }

    async Task<bool> ServeGet(Message msg)
    {
        return false;
    }

    async Task<bool> CheckPostJsonRequest(Message msg)
    {
        var url = msg.Url.SubstringUntil('?');
        var length = msg.RequestHeaders.GetValue("Content-Length")?.ParseInt();
        if (length.HasValue && server.Configuration.jsonPost != null && msg.Payload != null)
        {
            var request = new JsonRequest(url, msg.Payload, async str => await msg.SendStream(str, MimeTypes.ApplicationJson, (int)str.Length), keepAliveCancellation);
            return await server.Configuration.jsonPost(request);
        }
        else
            return false;
    }

    async Task<bool> ServeOptions(Message msg)
    {
        var request = msg.RequestHeaders.GetValue("Access-Control-Request-Headers");
        if (request != null)
            msg.AddResponseHeader("Access-Control-Allow-Headers", request);
        request = msg.RequestHeaders.GetValue("Access-Control-Request-Method");
        if (request != null)
            msg.AddResponseHeader("Access-Control-Allow-Methods", "*");
        if (server.Configuration.AccessControlMaxAgeStr != null)
        msg.AddResponseHeader("Access-Control-Max-Age", server.Configuration.AccessControlMaxAgeStr);
        await msg.SendOnlyHeaders();

        return true;
    }

    static int seedId;
    readonly Stopwatch stopwatch = new();

    bool isClosed;
    readonly CancellationToken keepAliveCancellation = new CancellationTokenSource(server.SocketLifetime).Token;

}
