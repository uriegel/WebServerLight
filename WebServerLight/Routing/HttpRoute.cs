namespace WebServerLight.Routing;

public class HttpRoute(List<Route> routes) : Route(routes)
{
    public static HttpRoute New() => new([]);

    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg.IsSecured == false
            ? await ProbeRoutes(msg)
            : RouteResult.Next;
}