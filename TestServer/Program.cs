using static System.Console;

using WebServerLight;

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .WebsiteFromResource("")
        .JsonPost(JsonPost)
        .Build();
    
server.Start();
ReadLine();
server.Stop();


async Task<bool> JsonPost(JsonRequest request)
{
    var data = await request.DeserializeAsync<Data>();
    return false;
} 

record Data(string Text, int Id);