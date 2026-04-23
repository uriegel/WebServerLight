namespace WebServerLight.Routing;

public class MethodRoute(Method method, List<Route> routes, Func<Exception, IRequest, Task>? onException = null) : Route(routes)
{
    public static MethodRoute New(Method method, Func<Exception, IRequest, Task>? onException = null) => new(method, [], onException);

    internal override async ValueTask<RouteResult> Probe(IRequest msg, Func<Exception, IRequest, Task>? probeOnException)
        => msg is Message message && message.Method == method
            ? await ProbeRoutes(msg, onException ?? probeOnException)
            : RouteResult.Next;
}