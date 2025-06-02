namespace WebServerLightSessions;

public enum RouteResult
{
    Next,
    Keepalive,
    Close,
    Detach
}