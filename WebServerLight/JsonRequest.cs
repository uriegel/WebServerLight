using System.Text.Json;

namespace WebServerLight;

public class JsonRequest(string url, Stream payload, CancellationToken cancellation)
{
    public string Url { get => url; }  
    public async Task<T?> DeserializeAsync<T>()
        => await JsonSerializer.DeserializeAsync<T>(payload, Json.Defaults, cancellation);
}