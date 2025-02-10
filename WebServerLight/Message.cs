using System.Collections.Immutable;
using System.Net.Sockets;
using System.Text;
using CsTools.Extensions;

using static System.Console;

namespace WebServerLight;

class Message : IRequest
{
    public Method Method { get; }
    public string Url { get; }

    public ImmutableDictionary<string, string> RequestHeaders { get; }

    public void AddResponseHeader(string key, string value)
    {
        ResponseHeaders.Remove(key);
        ResponseHeaders.Add(key, value);
    }

    public Dictionary<string, string> ResponseHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);


    public static async Task<Message?> Read(Server server, RequestSession requestSession, Stream networkStream, CancellationToken cancellation)
    {
        var buffer = new byte[8192];
        int bytesRead = 0;
        int totalBytes = 0;
        var headerBuffer = new MemoryStream(4096);
        while ((bytesRead = await networkStream.ReadAsync(buffer, cancellation)) > 0)
        {
            headerBuffer.Write(buffer, 0, bytesRead);
            totalBytes += bytesRead;

            // Convert buffer to string & check for header termination
            string headerText = Encoding.UTF8.GetString(headerBuffer.ToArray());

            int headerEndIndex = headerText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEndIndex < 0)
                headerEndIndex = headerText.IndexOf("\n\n", StringComparison.Ordinal);
            if (headerEndIndex >= 0)
            {
                headerEndIndex += headerText[headerEndIndex] == '\r' ? 4 : 2; // Adjust index for "\r\n\r\n" or "\n\n"
                return new Message(server, requestSession, networkStream, cancellation, headerText[..headerEndIndex], buffer, headerEndIndex);
            }
        }
        return null;
    }

    public Message(Server server, RequestSession requestSession, Stream networkStream, CancellationToken cancellation, string headerPart, byte[] buffer, int payloadBegin)
    {
        this.requestSession = requestSession;
        this.server = server;
        this.networkStream = networkStream;
        this.buffer = buffer;
        this.payloadBegin = payloadBegin;
        this.cancellation = cancellation;
        var parts = headerPart.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var method = parts[0].SubstringUntil(' ');
        Method = method switch
        {
            "GET" => Method.Get,
            "POST" => Method.Post,
            "PUT" => Method.Put,
            "Delete" => Method.Delete,
            _ => throw new Exception($"HTTP method {method} not supported")
        };
        Url = parts[0].StringBetween(" ", " ");
        RequestHeaders = parts.Skip(1).Select(MakeHeader).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

        // var length = RequestHeaders.GetValue("Content-Length")?.ParseInt();
        // if (length.HasValue)
        //     Fülle();

        // async void Fülle()
        // {
        //     using var stream = File.Create("affe.jpg");
        //     await stream.WriteAsync(buffer, payloadBegin, buffer.Length - payloadBegin);
        //     var diff = length.Value - buffer.Length + payloadBegin;
        //     var bytes = new byte[diff];
        //     var gut = await networkStream.ReadAsync(bytes);
        //     await stream.WriteAsync(bytes);
        // }
    }

    public async Task<bool> Receive()
    {
        try
        {
            if (Method == Method.Post
                    && string.Compare(RequestHeaders.GetValue("Content-Type"), "application/json", StringComparison.OrdinalIgnoreCase) == 0
                    && await CheckPostJsonRequest())
                return true;
            else if (server.Configuation.ResourceBasePath != null && await CheckResourceWebsite())
                return true;
            else
                await Send404();
            return true;
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.TimedOut)
            {
                Error.WriteLine($"{requestSession.Id} Socket session closed, Timeout has occurred");
                requestSession.Close(true);
                return false;
            }
            return true;
        }
        catch (ConnectionClosedException)
        {
            Error.WriteLine($"{requestSession.Id} Socket session closed via exception");
            requestSession.Close(true);
            return false;
        }
        catch (ObjectDisposedException oe)
        {
            Error.WriteLine($"{requestSession.Id} Socket session closed, an error has occurred: {oe}");
            requestSession.Close(true);
            return false;
        }
        catch (IOException ioe)
        {
            Error.WriteLine($"{requestSession.Id} Socket session closed: {ioe}");
            requestSession.Close(true);
            return false;
        }
        catch (Exception e)
        {
            Error.WriteLine($"{requestSession.Id} Socket session closed, an error has occurred while receiving: {e}");
            requestSession.Close(true);
            return false;
        }
    }

    async Task<bool> CheckResourceWebsite()
    {
        var url = Url.SubstringUntil('?');
        url = url != "/" ? url : "/index.html";
        var res = Resources.Get(url);
        if (res != null)
        {
            AddResponseHeader("Content-Length", $"{res.Length}");
            AddResponseHeader("Content-Type", url?.GetFileExtension()?.ToMimeType() ?? "text/html");
            await SendStream(res);
            return true;
        }
        else
            return false;
    }

    async Task<bool> CheckPostJsonRequest()
    {
        var url = Url.SubstringUntil('?');

        var length = RequestHeaders.GetValue("Content-Length")?.ParseInt();
        if (length.HasValue && server.Configuation.jsonPost != null)
        {
            // TODO deserialize from stream!
            var ms = new MemoryStream();
            await ms.WriteAsync(buffer, payloadBegin, Math.Min(buffer.Length - payloadBegin, (int)length));
            var diff = length.Value - buffer.Length + payloadBegin;
            if (diff > 0)
            {
                var bytes = new byte[diff];
                var gut = await networkStream.ReadAsync(bytes);
                await ms.WriteAsync(bytes);
            }
            ms.Position = 0;
            var request = new JsonRequest(url, ms, cancellation);
            return await server.Configuation.jsonPost(request);
        }
        else
            return false;
    }

    async Task SendStream(Stream stream)
    {
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
        await stream.CopyToAsync(networkStream);
        await stream.FlushAsync();
    }

    async Task Send404()
    {
        var body = "I can't find what you're looking for...";
        AddResponseHeader("Content-Length", $"{body.Length}");
        AddResponseHeader("Content-Type", "text/html");
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 404 Not Found\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n{body}"));
    }

    static KeyValuePair<string, string> MakeHeader(string headerLine)
    {
        var parts = headerLine.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new(parts[0], parts[1]);
    }

    readonly Server server;
    readonly RequestSession requestSession;
    readonly Stream networkStream;
    readonly byte[] buffer;
    readonly int payloadBegin;
    readonly CancellationToken cancellation;
}

// TODO JsonPostRequests
// TODO ResRequests
// TODO Commander test