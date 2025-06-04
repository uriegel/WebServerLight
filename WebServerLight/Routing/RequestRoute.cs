using WebServerLightSessions;

namespace WebServerLight.Routing;

class RequestRoute(Func<Message, ValueTask<RouteResult>> request) : Route([])
{
    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message
            ? await request(message)
            : RouteResult.Next;
}

