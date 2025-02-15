using System.Collections.Immutable;

namespace WebServerLight;

public class GetRequest(string url, Func<ImmutableDictionary<string, string>> getQueryParts, Func<Stream, int, string, Task> sendData)
{
    public string Url { get => url; }
    public ImmutableDictionary<string, string> QueryParts { get => getQueryParts(); }

    // TODO addResponseHeader
    public async Task SendAsync(Stream payload, int contentLength, string contentType)
        => await sendData(payload, contentLength, contentType);
}