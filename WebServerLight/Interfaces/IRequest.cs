using System.Collections.Immutable;

namespace WebServerLight;

public interface IRequest
{
    Method Method { get; }
    string Url { get; }
    ImmutableDictionary<string, string> RequestHeaders { get; }
}