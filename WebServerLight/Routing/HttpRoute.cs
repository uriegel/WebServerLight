namespace WebServerLight.Routing;

public class HttpRoute(List<Route> routes) : Route(routes)
{
    public static HttpRoute New() => new([]);

    internal override async ValueTask<RouteResult> Probe(IRequest msg, Func<Exception, IRequest, Task>? onException)
        => msg.IsSecured == false
            ? await ProbeRoutes(msg, onException)
            : RouteResult.Next;
}