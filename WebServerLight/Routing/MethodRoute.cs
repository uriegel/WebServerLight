
namespace WebServerLight.Routing;

class MethodRoute(Method method, List<Route> routes) : Route(routes)
{
    public MethodRoute(Method method) : this(method, []) { }
    public override async ValueTask<bool> Probe(Message msg)
        => msg.Method == method && await ProbeRoutes(msg);
}