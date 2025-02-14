namespace WebServerLight.Routing;

class Route(List<Route> routes)
{
    public virtual ValueTask<bool> Probe(Message msg) => ProbeRoutes(msg);

    public IEnumerable<Route> Routes { get => routes; }

    protected ValueTask<bool> ProbeRoutes(Message msg)
        => Routes
                .ToAsyncEnumerable()
                .SelectAwait(async n => await n.Probe(msg))
                .FirstOrDefaultAsync(n => n == true);
}