using CsTools.Extensions;
using WebServerLightSessions;

namespace WebServerLight.Routing;

class UpdateRoute() : Route([])
{
    public async override ValueTask<RouteResult> Probe(Message msg)
    {
        var upgrade = msg.RequestHeaders.GetValue("upgrade");
        return upgrade != null && string.Compare(upgrade, "websocket", true) == 0
            ? await RouteResult.Detach.SideEffectAsync(async _ => await msg.UpgradeWebSocket())
            : RouteResult.Next;
    }
}
