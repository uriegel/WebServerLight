using static System.Console;

namespace WebServerLight;

class Server : IServer
{
    public void Start()
    {
        WriteLine("Starting...");
        WriteLine("Started");
    }

    public void Stop()
    {
        WriteLine("Stopping...");
        WriteLine("Stopped");
    }
}