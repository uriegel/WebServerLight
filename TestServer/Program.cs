using static System.Console;

using WebServerLight;
using CsTools.Extensions;

WriteLine(@"Test site:  http://localhost:8080");

var server =
    ServerBuilder
        .New()
        .Http(8080)
        .WebsiteFromResource()
        .Get(Get)
        .JsonPost(JsonPost)
        .AddAllowedOrigin("http://localhost:8080")
        .AccessControlMaxAge(TimeSpan.FromMinutes(1))
        .Build();
    
server.Start();
ReadLine();
server.Stop();

async Task<bool> Get(GetRequest request)
{
    if (request.Url == "/image")
    {
        var res = Resources.Get("image");
        if (res != null)
        {
            await request.SendAsync(res, (int)res.Length, MimeTypes.ImageJpeg);
            return true;
        }
        else
            return false;
    }
    else
        return false;
}

async Task<bool> JsonPost(JsonRequest request)
{
    if (request.Url == "/json/cmd4")
    {
        var response = new Response([
            new Contact("Uwe Riegel", 34),
            new Contact("Miles Davis", 90),
            new Contact("John Coltrane", 99)], 123, "Response without input");
        await request.SendAsync(response);
    }
    else
    {
        var data = await request.DeserializeAsync<Data>();
        var response = new Response([
            new Contact("Uwe Riegel", 34),
            new Contact("Miles Davis", 90),
            new Contact("John Coltrane", 99)], 123, request.Url);

        await request.SendAsync(response);
    }
    return true;
} 

record Data(string Text, int Id, IEnumerable<string> LongArray);
record Response(IEnumerable<Contact> Contacts, int ID, string Name);
record Contact(string Name, int Id);
