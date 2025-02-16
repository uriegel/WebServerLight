using static System.Console;

using WebServerLight;
using CsTools.Extensions;
using CsTools;

WriteLine(@"Test site:  http://localhost:8080");

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .WebsiteFromResource()
        .Get(Get)
        .JsonPost(JsonPost)
        .WebSocket(WebSocket)
        .AddAllowedOrigin("http://localhost:8080")
        .AccessControlMaxAge(TimeSpan.FromMinutes(1))
        .UseRange()
        .Build();
    
server.Start();
ReadLine();
server.Stop();

async Task<bool> Get(IRequest request)
{
    if (request.Url == "/image")
    {
        var res = Resources.Get("image");
        if (res != null)
        {
            await request.SendAsync(res, res.Length, MimeTypes.ImageJpeg);
            return true;
        }
        else
            return false;
    }
    else if (request.Url == "/video")
    {
        using var video = File.OpenRead("/daten/Videos/2010.mp4");
        if (video != null)
        {
            await request.SendAsync(video, video.Length, MimeType.Get(video.Name.GetFileExtension() ?? ".txt") ?? MimeTypes.TextPlain);
            return true;
        }
        else
            return false;
    }
    else
        return false;
}

async Task<bool> JsonPost(IRequest request)
{
    if (request.Url == "/json/cmd4")
    {
        var response = new Response([
            new Contact("Uwe Riegel", 34),
            new Contact("Miles Davis", 90),
            new Contact("John Coltrane", 99)], 123, "Response without input");
        await request.SendJsonAsync(response);
    }
    else
    {
        var data = await request.DeserializeAsync<Data>();
        var response = new Response([
            new Contact("Uwe Riegel", 34),
            new Contact("Miles Davis", 90),
            new Contact("John Coltrane", 99)], 123, request.Url);

        await request.SendJsonAsync(response);
    }
    return true;
} 

async void WebSocket(IWebSocket webSocket)
{
    var i = 0;
    while (true)
    {
        await Task.Delay(5000);
        await webSocket.SendString($"Web socket text: {++i}");
    }
}

record Data(string Text, int Id, IEnumerable<string> LongArray);
record Response(IEnumerable<Contact> Contacts, int ID, string Name);
record Contact(string Name, int Id);
