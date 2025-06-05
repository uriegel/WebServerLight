using WebServerLightSessions;

namespace WebServerLight.Routing;

public class HttpsRoute(List<Route> routes) : Route(routes)
{
    public static HttpsRoute New() => new([]);

    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg.IsSecured == true
            ? await ProbeRoutes(msg)
            : RouteResult.Next;
}