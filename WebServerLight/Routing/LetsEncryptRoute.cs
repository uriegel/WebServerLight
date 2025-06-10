using CsTools.Extensions;
using CsTools.Functional;

namespace WebServerLight.Routing;

class LetsEncryptRoute() : Route([])
{
    internal override async ValueTask<RouteResult> Probe(IRequest msg)
        => msg is Message message && message.Url.StartsWith("/.well-known/acme-challenge")
            ? await Request(message)
            : RouteResult.Next;

    static async Task<RouteResult> Request(Message msg)
    {
        var text = msg.Url.EndsWith("check")
            ? "checked"
            : "LETS_ENCRYPT_DIR"
                .GetEnvironmentVariable()
                .AppendPath(msg.Url[28..])
                .ReadAllTextFromFilePath()
                .GetOrDefault("");

        await msg.SendText(text);
        return RouteResult.Keepalive;
    }
}

