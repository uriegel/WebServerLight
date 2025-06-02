
using WebServerLightSessions;

namespace WebServerLight.Routing;

class MethodRoute(Method method, List<Route> routes) : Route(routes)
{
    public MethodRoute(Method method) : this(method, []) { }
    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message && message.Method == method
            ? await ProbeRoutes(msg)
            : RouteResult.Next;
}