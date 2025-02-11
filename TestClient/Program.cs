using CsTools.HttpRequest;

using static System.Console;
using static CsTools.HttpRequest.Core;


// var jsonGetRequest = new JsonRequest("http://localhost:8080");
// var result2 = jsonGetRequest.Get<FileType[]>("/getfiles", true);
// var res22 = await result2.ToResult();
var msgp = await Request.RunAsync(DefaultSettings with
            {
                Method = HttpMethod.Post,
                BaseUrl = "http://localhost:8080",
                Url = "/putfile/Pictures/pic.jpg",
                AddContent = () => new StreamContent(File.OpenRead("image.jpg"), 8100)
            }, false);
WriteLine($"Der Code: {msgp.StatusCode}");
ReadLine();