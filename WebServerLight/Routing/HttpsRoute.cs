namespace WebServerLight.Routing;

public class HttpsRoute(List<Route> routes) : Route(routes)
{
    public static HttpsRoute New() => new([]);

    internal override async ValueTask<RouteResult> Probe(IRequest msg, Func<Exception, IRequest, Task>? onException)
        => msg.IsSecured == true
            ? await ProbeRoutes(msg, onException)
            : RouteResult.Next;
}