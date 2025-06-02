
using CsTools.Extensions;
using WebServerLightSessions;

namespace WebServerLight.Routing;

public class SubpathRoute(string subPath, List<Route> routes) : Route(routes)
{
    public static SubpathRoute New(string subPath) => new(subPath);

    internal SubpathRoute(string subPath) : this(subPath, []) { }

    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message && message.Url.StartsWith(subPath, StringComparison.CurrentCultureIgnoreCase)
                ? await ProbeRoutes(msg)
                : RouteResult.Next;
}