using System.Collections.Immutable;

namespace WebServerLight;

public interface IRequest
{
    string Url { get; }
    ImmutableDictionary<string, string> QueryParts { get; }

    void AddResponseHeader(string key, string value);

    Task SendAsync(Stream payload, long contentLength, string contentType);

    Task<T?> DeserializeAsync<T>();

    Task SendJsonAsync<T>(T t);

    Task Send404();
}