using static System.Console;

using WebServerLight;

WriteLine(@"Test site:  http://localhost:8080");

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
    var response = new Response([
        new Contact("Uwe Riegel", 34),
        new Contact("Miles Davis", 90),
        new Contact("John Coltrane", 99)], 123, "Response");

    await request.SendAsync(response);
    return true;
} 

record Data(string Text, int Id, IEnumerable<string> LongArray);
record Response(IEnumerable<Contact> Contacts, int ID, string Name);
record Contact(string Name, int Id);
