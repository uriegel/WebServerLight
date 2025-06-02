using WebServerLightSessions;

namespace WebServerLight.Routing;

public class Route(List<Route> routes)
{
    internal virtual ValueTask<RouteResult> Probe(IRequest msg) => ProbeRoutes(msg);

    public IEnumerable<Route> Routes { get => routes; }

    protected ValueTask<RouteResult> ProbeRoutes(IRequest msg)
        => Routes
                .ToAsyncEnumerable()
                .SelectAwait(async n => await n.Probe(msg))
                .FirstOrDefaultAsync(n => n != RouteResult.Next);
}