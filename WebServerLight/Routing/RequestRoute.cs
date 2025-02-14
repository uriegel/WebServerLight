
using WebServerLightSessions;

namespace WebServerLight.Routing;

class RequestRoute(Func<Message, ValueTask<RouteResult>> request) : Route([])
{
    public override ValueTask<RouteResult> Probe(Message msg) => request(msg);
}

