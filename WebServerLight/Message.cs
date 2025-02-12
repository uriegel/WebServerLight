using System.Collections.Immutable;
using System.Text;
using CsTools.Extensions;

namespace WebServerLight;

class Message(Server server, Method method, string url, ImmutableDictionary<string, string> requestHeaders, Stream networkStream, Memory<byte> payloadBegin) 
{
    public Method Method { get => method; }
    public string Url { get => url; }

    public PayloadStream? Payload {  get => _Payload ??= GetPayload(); }
    PayloadStream? _Payload;

    public ImmutableDictionary<string, string> RequestHeaders { get => requestHeaders; }

    public void AddResponseHeader(string key, string value)
    {
        ResponseHeaders.Remove(key);
        ResponseHeaders.Add(key, value);
    }

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

                return new Message(server, method, url, requestHeaders, networkStream, new Memory<byte>(buffer, headerEndIndex, totalBytes - headerEndIndex));
            }
        }
        return null;
    }

    public async Task SendStream(Stream stream, string contentType, int length)
    {
        AddResponseHeader("Content-Length", $"{length}");
        AddResponseHeader("Content-Type", contentType);
        InitResponseHeaders(true);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
        await stream.CopyToAsync(networkStream);
        await stream.FlushAsync();
    }

    public async Task Send404()
    {
        var body = "I can't find what you're looking for...";
        InitResponseHeaders(true);
        AddResponseHeader("Content-Length", $"{body.Length}");
        AddResponseHeader("Content-Type", MimeTypes.TextPlain);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 404 Not Found\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n{body}"));
    }

    public async Task SendOnlyHeaders()
    {
        InitResponseHeaders(false);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
    }

    PayloadStream? GetPayload() 
    {
        var length = RequestHeaders.GetValue("Content-Length")?.ParseInt();
        if (length.HasValue)
            return new PayloadStream(payloadBegin, networkStream, length.Value);  
        else
            return null;
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
}

