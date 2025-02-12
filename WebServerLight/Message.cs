using System.Collections.Immutable;
using System.Text;
using CsTools.Extensions;

namespace WebServerLight;

class Message(Method method, string url, ImmutableDictionary<string, string> requestHeaders, Stream networkStream, Memory<byte> payloadBegin) 
    : IRequest
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

    public static async Task<Message?> Read(Stream networkStream, CancellationToken cancellation)
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

                return new Message(method, url, requestHeaders, networkStream, new Memory<byte>(buffer, headerEndIndex, totalBytes - headerEndIndex));
            }
        }
        return null;
    }

    public async Task SendStream(Stream stream, string contentType, int length)
    {
        AddResponseHeader("Content-Length", $"{length}");
        AddResponseHeader("Content-Type", contentType);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n"));
        await stream.CopyToAsync(networkStream);
        await stream.FlushAsync();
    }

    public async Task Send404()
    {
        var body = "I can't find what you're looking for...";
        AddResponseHeader("Content-Length", $"{body.Length}");
        AddResponseHeader("Content-Type", MimeTypes.TextPlain);
        await networkStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 404 Not Found\r\n{string.Join("\r\n", ResponseHeaders.Select(n => $"{n.Key}: {n.Value}"))}\r\n\r\n{body}"));
    }

    PayloadStream? GetPayload() 
    {
        var length = RequestHeaders.GetValue("Content-Length")?.ParseInt();
        if (length.HasValue)
            return new PayloadStream(payloadBegin, networkStream, length.Value);  
        else
            return null;
    }

    static KeyValuePair<string, string> MakeHeader(string headerLine)
    {
        var parts = headerLine.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new(parts[0], parts[1]);
    }
}

// TODO preflight and Cors cache
// TODO ResRequests