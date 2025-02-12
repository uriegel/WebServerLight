namespace WebServerLight;

public class GetRequest(string url, Func<Stream, int, string, Task> sendData)
{
    public string Url { get => url; }

    // TODO addResponseHeader
    public async Task SendAsync(Stream payload, int contentLength, string contentType)
        => await sendData(payload, contentLength, contentType);
}