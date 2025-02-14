namespace WebServerLightSessions;

public enum RouteResult
{
    Keepalive,
    Close,
    Next,
    Detach
}