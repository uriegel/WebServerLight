using CsTools;
using CsTools.Extensions;
using WebServerLight;
using WebServerLight.Routing;

using static System.Console;

WriteLine(@"Test site:  http://localhost:8080");

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .WebsiteFromResource()
        .Route(MethodRoute
                .New(Method.Get)
                .Add(PathRoute
                        .New("/image")
                        .Request(GetImage))
                .Add(PathRoute
                        .New("/video")
                        .Request(GetVideo)))
        .Route(MethodRoute
                .New(Method.Post)
                .Add(PathRoute
                        .New("/json/cmd4")
                        .Request(JsonPost4))
                .Add(PathRoute
                        .New("/json")
                        .Request(JsonPost)))
        .Route(PathRoute
                .New("/media")
                .Add(MethodRoute
                    .New(Method.Get)
                    .Request(GetMedia)
                    .Request(GetMediaFile)))

        // TODO HostPath (illmatic)
        // TODO JsonPost
        .WebSocket(WebSocket)
        .AddAllowedOrigin("http://localhost:8080")
        .AccessControlMaxAge(TimeSpan.FromMinutes(1))
        .UseRange()
        .Build();
    
server.Start();
ReadLine();
server.Stop();

async Task<bool> GetMedia(IRequest request)
{
    var path = "/daten/Videos".AppendPath(request.SubPath);
    if (request.SubPath?.Contains('.') == true)
        return false;
    WriteLine($"GetMedia: {path}");
    var info = new DirectoryInfo(path);
    if (!info.Exists)
        return false;
    var json = new DirectoryContent(
        [.. info.GetDirectories().Select(n => n.Name).OrderBy(n => n)],
        [.. info.GetFiles().Select(n => n.Name).OrderBy(n => n)]
    );
    await request.SendJsonAsync(json);
    return true;
}

async Task<bool> GetMediaFile(IRequest request)
{
    var path = "/daten/Videos".AppendPath(request.SubPath);
    if (request.SubPath?.Contains('.') != true || !File.Exists(path))
        return false;
    WriteLine($"GetMediaFile: {path}, {File.Exists(path)}");


    using var video = File.OpenRead(path);
    if (video != null)
    {
        await request.SendAsync(video, video.Length, MimeType.Get(".mp4") ?? MimeTypes.TextPlain);
        return true;
    }
    else
        return false;
}

async Task<bool> GetImage(IRequest request)
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

async Task<bool> GetVideo(IRequest request)
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

async Task<bool> JsonPost4(IRequest request)
{
    var response = new Response([
        new Contact("Uwe Riegel", 34),
        new Contact("Miles Davis", 90),
        new Contact("John Coltrane", 99)], 123, "Response without input");
    await request.SendJsonAsync(response);
    return true;
} 

async Task<bool> JsonPost(IRequest request)
{
    var data = await request.DeserializeAsync<Data>();
    var response = new Response([
        new Contact("Uwe Riegel", 34),
        new Contact("Miles Davis", 90),
        new Contact("John Coltrane", 99)], 123, request.Url);

    await request.SendJsonAsync(response);
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
record DirectoryContent(string[] Directories, string[] Files);