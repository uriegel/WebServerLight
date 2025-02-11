using System.Collections.Immutable;
using System.Net.Sockets;
using System.Text;
using CsTools.Extensions;

using static System.Console;

namespace WebServerLight;

class Message(Server server, RequestSession requestSession, Stream networkStream, Method method, string url, 
    ImmutableDictionary<string, string> requestHeaders, Memory<byte> payloadBegin, CancellationToken cancellation) 
    : IRequest
{
    public Method Method { get => method; }
    public string Url { get => url; }

    public ImmutableDictionary<string, string> RequestHeaders { get => requestHeaders; }

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
                var headerPart = headerText[..headerEndIndex];
                headerEndIndex += headerText[headerEndIndex] == '\r' ? 4 : 2; // Adjust index for "\r\n\r\n" or "\n\n"
                var parts = headerPart.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                var method = parts[0].SubstringUntil(' ') switch
                {
                    "GET" => Method.Get,
                    "POST" => Method.Post,
                    "PUT" => Method.Put,
                    "Delete" => Method.Delete,
                    var m => throw new Exception($"HTTP method {m} not supported")
                };
                var url = parts[0].StringBetween(" ", " ");
                var requestHeaders = parts.Skip(1).Select(MakeHeader).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

                return new Message(server, requestSession, networkStream, method, url, requestHeaders, 
                    new Memory<byte>(buffer, headerEndIndex, totalBytes - headerEndIndex), cancellation);
            }
        }
        return null;
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
            var request = new JsonRequest(url, new PayloadStream(payloadBegin, networkStream, length.Value), SendData, cancellation);
            return await server.Configuation.jsonPost(request);
        }
        else
            return false;

        async Task SendData(Stream payload)
        {
            AddResponseHeader("Content-Length", $"{payload.Length}");
            AddResponseHeader("Content-Type", "application/json");
            await SendStream(payload);
        }
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
}

// TODO Receive to RequestSession
// TODO StreamBase to CsTools
// TODO PayloadStream with cmd2: array of strings, 20000, read rest from networkStream
// TODO ResRequests