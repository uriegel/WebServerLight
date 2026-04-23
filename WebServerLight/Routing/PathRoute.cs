using CsTools.Extensions;

namespace WebServerLight.Routing;

public class PathRoute(string path, List<Route> routes) : Route(routes)
{
    public static PathRoute New(string path) => new(path);

    internal PathRoute(string path) : this(path, []) { }

    internal override async ValueTask<RouteResult> Probe(IRequest msg, Func<Exception, IRequest, Task>? onException)
        => msg is Message message && message.Url.StartsWith(path, StringComparison.CurrentCultureIgnoreCase)
                ? await ProbeRoutes(msg.SideEffect(n => (n as Message)?.SetRequestPath(path)), onException)
                : RouteResult.Next;
}