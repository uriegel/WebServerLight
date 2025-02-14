
namespace WebServerLight.Routing;

class RequestRoute(Func<Message, ValueTask<bool>> request) : Route([])
{
    public override ValueTask<bool> Probe(Message msg) => request(msg);
}

