using static System.Console;

using WebServerLight;

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .WebsiteFromResource("")
        .Build();
    
server.Start();
ReadLine();
server.Stop();


