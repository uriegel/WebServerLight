using static System.Console;

using WebServerLight;

var server = ServerBuilder.New().Build();
server.Start();


WriteLine("working...");
ReadLine();



server.Stop();


