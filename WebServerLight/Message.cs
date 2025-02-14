using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using CsTools.Extensions;
using Microsoft.VisualBasic;
using WebServerLight.Sessions;
using WebServerLight.Streams;

namespace WebServerLight;

class Message(Server server, Method method, string url, ImmutableDictionary<string, string> requestHeaders, Stream networkStream, Memory<byte> payloadBegin, CancellationToken keepAliveCancellation)
{
    public Method Method { get => method; }
    public string Url { get => url; }

    public PayloadStream? Payload { get => _Payload ??= GetPayload(); }
    PayloadStream? _Payload;

    public ImmutableDictionary<string, string> RequestHeaders { get => requestHeaders; }

    public void AddResponseHeader(string key, string value)
    {
        ResponseHeaders.Remove(key);
        ResponseHeaders.Add(key, value);
    }

    public Server Server { get => server; }

    public CancellationToken KeepAliveCancellation { get => keepAliveCancellation; }

    public Dictionary<string, string> ResponseHeaders { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<Message?> Read(Server server, Stream networkStream, CancellationToken cancellation)
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
                    "DELETE" => Method.Delete,
                    "OPTIONS" => Method.Options,
                    var m => throw new Exception($"HTTP method {m} not supported")
                };
                var url = parts[0].StringBetween(" ", " ");
                var requestHeaders = parts.Skip(1).Select(MakeHeader).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

                return new Message(server, method, url, requestHeaders, networkStream, new Memory<byte>(buffer, headerEndIndex, totalBytes - headerEndIndex), cancellation);
            }
        }
        return null;
    }

    public async Task SendStream(Stream stream, string contentType, int length, CancellationToken keepAliveCancellation)
    {
        AddResponseHeader("Content-Length", $"{length}");
        AddResponseHeader("Content-Type", contentType);
        InitResponseHeaders(true);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"), keepAliveCancellation);
        await stream.CopyToAsync(networkStream, keepAliveCancellation);
        await stream.FlushAsync(keepAliveCancellation);
    }

    public async ValueTask<bool> Send(string body, CancellationToken keepAliveCancellation)
    {
        InitResponseHeaders(true);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 404 Not Found\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n{body}"), keepAliveCancellation);
        return true;
    }

    public async Task SendOnlyHeaders(int code = 204, string status = "204 No Content")
    {
        InitResponseHeaders(false);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 {code} {status}\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
    }

    public async Task UpgradeWebSocket()
    {
        var secKey = RequestHeaders.GetValue("sec-websocket-key");
        var userAgent = RequestHeaders.GetValue("User-Agent");
        var extensions = RequestHeaders.GetValue("sec-websocket-extensions")?.Split([';']) ?? [];
        var supportedExtensions = new Extensions(
            extensions.Contains("permessage-deflate")
                && userAgent?.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) != true
                && userAgent?.Contains("iPhone", StringComparison.OrdinalIgnoreCase) != true
                && userAgent?.Contains("iPad", StringComparison.OrdinalIgnoreCase) != true, extensions.Contains("watchdog"));
        var extensionsHeader = supportedExtensions.PerMessageDeflate || supportedExtensions.WatchDog
            ? $"\r\nSec-WebSocket-Extensions: {string.Join("; ", GetExtensions(supportedExtensions))}"
            : "";
        secKey += webSocketKeyConcat;
        var hashKey = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(secKey));
        var base64Key = Convert.ToBase64String(hashKey);

        AddResponseHeader("Connection", "Upgrade");
        AddResponseHeader("Upgrade", "websocket");
        AddResponseHeader("Sec-WebSocket-Accept", base64Key + extensionsHeader);
        await SendOnlyHeaders(101, "Switching Protocols");
        server.Configuration.onWebSocket?.Invoke(new WebSocketSession(Url, networkStream, supportedExtensions));
        
        static IEnumerable<string> GetExtensions(Extensions extensions)
        {
            if (extensions.PerMessageDeflate)
            {
                yield return "permessage-deflate";
                yield return "client_no_context_takeover";
            }
            if (extensions.WatchDog)
                yield return "watchdog";
        }
    }

    PayloadStream? GetPayload()
    {
        var length = RequestHeaders.GetValue("Content-Length")?.ParseInt();
        return length.HasValue
            ? new PayloadStream(payloadBegin, networkStream, length.Value)
            : null;
    }

    void InitResponseHeaders(bool payload)
    {
        if (!ResponseHeaders.ContainsKey("Content-Length") && payload)
            AddResponseHeader("Connection", "close");
        AddResponseHeader("Date", DateTime.Now.ToUniversalTime().ToString("R"));
        AddResponseHeader("Server", "URiegel WebServerLight");

        // if (server.Configuration.XFrameOptions != XFrameOptions.NotSet)
        //     headers["X-Frame-Options"] = server.Configuration.XFrameOptions.ToString();
        // if (server.Configuration.HstsDurationInSeconds > 0)
        //     headers["Strict-Transport-Security"] = $"max-age={server.Configuration.HstsDurationInSeconds}";

        if (server.Configuration.AllowedOrigins.Count > 0)
        {
            var origin = requestHeaders.GetValue("origin");
            if (!string.IsNullOrEmpty(origin))
            {
                var host = requestHeaders.GetValue("host");
                if (string.Compare(origin, host, true) != 0)
                {
                    var originToAllow = server.Configuration.AllowedOrigins.FirstOrDefault(n => string.Compare(n, origin, true) == 0);
                    if (originToAllow != null)
                        AddResponseHeader("Access-Control-Allow-Origin", originToAllow);
                }
            }
        }
    }

    static KeyValuePair<string, string> MakeHeader(string headerLine)
        => new(
            headerLine.SubstringUntil(':'),
            headerLine.SubstringAfter(':').Trim()
        );
        
    const string webSocketKeyConcat = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";        
}

