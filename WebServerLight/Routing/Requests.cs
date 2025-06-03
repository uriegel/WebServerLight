using CsTools.Extensions;
using WebServerLightSessions;

namespace WebServerLight.Routing;

static class Requests
{
    public static async ValueTask<RouteResult> Send404(Message msg)
    {
        var body = "I can't find what you're looking for...";
        msg.AddResponseHeader("Content-Length", $"{body.Length}");
        msg.AddResponseHeader("Content-Type", MimeTypes.TextPlain);
        await msg.Send(body, msg.KeepAliveCancellation);
        return RouteResult.Keepalive;
    }

    public static async ValueTask<RouteResult> ServeResourceWebsite(Message msg)
    {
        var url = msg.Url;
        url = url != "/" ? url : "/index.html";
        var res = Resources.Get(url);
        if (res != null)
        {
            await msg.SendStream(res, url?.GetFileExtension()?.ToMimeType() ?? MimeTypes.TextHtml, (int)res.Length, msg.KeepAliveCancellation);
            return RouteResult.Keepalive;
        }
        else
            return RouteResult.Next;
    }

    public static Func<Message, ValueTask<RouteResult>> ServeRequest(Func<IRequest, Task<bool>> request)
    {
        return ServeRequest;
        
        async ValueTask<RouteResult> ServeRequest(Message msg)
        {
            try
            {
                return await request(msg)
                    ? RouteResult.Keepalive
                    : RouteResult.Next;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ServerRequest failed: {e}");
                return RouteResult.Next;
            }
        }
    }
    
    public static async ValueTask<RouteResult> ServeOptions(Message msg)
    {
        var request = msg.RequestHeaders.GetValue("Access-Control-Request-Headers");
        if (request != null)
            msg.AddResponseHeader("Access-Control-Allow-Headers", request);
        request = msg.RequestHeaders.GetValue("Access-Control-Request-Method");
        if (request != null)
            msg.AddResponseHeader("Access-Control-Allow-Methods", "*");
        if (msg.Server.Configuration.AccessControlMaxAgeStr != null)
            msg.AddResponseHeader("Access-Control-Max-Age", msg.Server.Configuration.AccessControlMaxAgeStr);
        await msg.SendOnlyHeaders();
        return RouteResult.Keepalive;
    }
}