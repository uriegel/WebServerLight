namespace WebServerLight.Routing;

class RequestRoute(Func<Message, ValueTask<RouteResult>> request) : Route([])
{
    internal override async ValueTask<RouteResult> Probe(IRequest msg, Func<Exception, IRequest, Task>? onException)
    {
        try
        {
            return msg is Message message
                ? await request(message)
                : RouteResult.Next;
        }
        catch (Exception e)
        {
            if (onException != null)
            {
                await onException(e, msg);
                return RouteResult.Keepalive;
            }
            else
                throw;
        }
    }
}

