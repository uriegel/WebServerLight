namespace WebServerLight.Routing;

public enum RouteResult
{
    Next,
    Keepalive,
    Close,
    Detach
}