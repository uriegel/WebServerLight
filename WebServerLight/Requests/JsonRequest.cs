using System.Text.Json;

namespace WebServerLight;

public class JsonRequest(string url, Stream payload, Func<Stream, Task> sendData, CancellationToken cancellation)
{
    public string Url { get => url; }
    public async Task<T?> DeserializeAsync<T>()
        => await JsonSerializer.DeserializeAsync<T>(payload, Json.Defaults, cancellation);

    public async Task SendAsync<T>(T t)
    {
        var ms = new MemoryStream();
        await JsonSerializer.SerializeAsync(ms, t, Json.Defaults, cancellation);
        ms.Position = 0;
        await sendData(ms);
    }
}