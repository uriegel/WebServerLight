using System.Collections.Immutable;

namespace WebServerLight;

public interface IRequest
{
    bool IsSecured { get; }
    string Url { get; }

    string? SubPath { get; }

    ImmutableDictionary<string, string> RequestHeaders { get; }
    ImmutableDictionary<string, string> QueryParts { get; }

    void AddResponseHeader(string key, string value);
    Task SendAsync(Stream payload, long contentLength, string contentType);
    Task SendTextAsync(string body);

    Task<T?> DeserializeAsync<T>();

    Task SendJsonAsync<T>(T t);
    Task SendJsonAsync(object obj, Type objType);

    Task Send404Async();
}