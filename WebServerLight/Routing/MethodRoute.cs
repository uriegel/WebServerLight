namespace WebServerLight.Routing;

public class MethodRoute(Method method, List<Route> routes) : Route(routes)
{
    public static MethodRoute New(Method method) => new(method);
    MethodRoute(Method method) : this(method, []) { }

    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message && message.Method == method
            ? await ProbeRoutes(msg)
            : RouteResult.Next;
}