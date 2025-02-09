using static System.Console;

using WebServerLight;

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .Build();
    
server.Start();
ReadLine();
server.Stop();


