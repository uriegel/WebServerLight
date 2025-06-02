using WebServerLightSessions;

namespace WebServerLight.Routing;

public class PathExactRoute(string path, List<Route> routes) : Route(routes)
{
    public static PathExactRoute New(string path) => new(path);

    internal PathExactRoute(string path) : this(path, []) { }

    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message && string.Compare(message.Url, path, StringComparison.CurrentCultureIgnoreCase) == 0
                ? await ProbeRoutes(msg)
                : RouteResult.Next;
}