using CsTools.Extensions;

namespace WebServerLight.Routing;

class UpdateRoute() : Route([])
{
    internal async override ValueTask<RouteResult> Probe(IRequest msg)
    {
        if (msg is Message message)
        {
            var upgrade = message.RequestHeaders.GetValue("upgrade");
            return upgrade != null && string.Compare(upgrade, "websocket", true) == 0
                ? await RouteResult.Detach.SideEffectAsync(async _ => await message.UpgradeWebSocket())
                : RouteResult.Next;
        }
        else
            return RouteResult.Next;
    }
}
