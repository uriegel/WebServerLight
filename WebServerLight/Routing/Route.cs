using CsTools.Extensions;
using WebServerLightSessions;

namespace WebServerLight.Routing;

public class Route(List<Route> routes)
{
    public Route Add(Route route) => this.SideEffect(n => routes.Add(route));

    public Route Request(Func<IRequest, Task<bool>> request)
        => this.SideEffect(n => routes.Add(new RequestRoute(Requests.ServeRequest(request))));

    internal virtual ValueTask<RouteResult> Probe(IRequest msg) => ProbeRoutes(msg);

    internal IEnumerable<Route> Routes { get => routes; }

    protected ValueTask<RouteResult> ProbeRoutes(IRequest msg)
        => Routes
                .ToAsyncEnumerable()
                .SelectAwait(async n => await n.Probe(msg))
                .FirstOrDefaultAsync(n => n != RouteResult.Next);
}