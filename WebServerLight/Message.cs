using System.Collections;
using System.Collections.Immutable;
using System.Text;
using CsTools.Extensions;

namespace WebServerLight;

class Message
{
    public Method Method { get; }
    public string Url { get; }
    public static async Task<Message?> Read(Server server, Stream networkStream, CancellationToken cancellation)
    {
        var buffer = new byte[8192];
        int bytesRead = 0;
        int totalBytes = 0;
        var headerBuffer = new MemoryStream(4096);
        while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellation)) > 0)
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
                return new Message(headerText[..headerEndIndex], buffer, headerEndIndex);
            }
        }
        return null;
    }

    public Message(string headerPart, byte[] buffer, int payloadBegin)
    {
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
        var affen = parts.Skip(1).Select(MakeHeader).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        var affe = affen;
        var länge = affen.GetValueOrDefault("affe");
         länge = affen.GetValueOrDefault("Content-Length");
         länge = affen.GetValueOrDefault("content-length");
    }

    static KeyValuePair<string, string> MakeHeader(string headerLine)
    {
        var parts = headerLine.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new(parts[0], parts[1]);
    }
}