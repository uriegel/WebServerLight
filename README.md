# WebServerLight
A C# light weight web server

# Table of contents 
1. [Setup](#setup)
2. [Configuration](#configuration)
    1. [Enabling HTTP and/or HTTPS](#httpenabling)
    2. [Certificate for HTTPS](#certificate)
    3. [Serving a web site from .NET resource](#websitefromresource)
    4. [Use range requests for streaming media files like videos](#userange)
3. [Routing](#routing)
4. [Serving requests](#servingrequests)
5. [WebSockets](#websockets)

## Setup <a name="setup"></a>

In a C# Console program, you have to add this package to your project:

```cs
<ItemGroup>
    <PackageReference Include="WebServerLight" Version="1.0.0" />
</ItemGroup>
```
Now you can set up your web server with the builder pattern:

```cs
var server =
    WebServer
        .New()
        ...
        .Build();

server.Start();
ReadLine();
server.Stop();

```

## Configuration <a name="configuration"></a>
The Web Server can be configured with the help of many configuration functions

### Enabling HTTP and/or HTTPS <a name="httpenabling"></a>

Insecure HTTP can be enabled like this:


```cs
var server =
    WebServer
        .New()
        .Http()
        .Build();
```

or specifying another HTTP port:


```cs
WebServer
    .New()
    .Http(8080)

```
Secure HTTPS can be enabled like this:

```cs
var server =
    WebServer
        .New()
        .Https(4433)
        .Build();
```
### Certificate for HTTPS <a name="certificate"></a>

There are two ways for providing a HTTPS Certificate:

1. Configuring a .pfx file containig the certificate
2. Automatically requesting a certificate via Let's Encrypt

If you have a valid pfx certificate with private key inxcluded, you can configure the web server to use this certificate:


```cs
var server =
    WebServer
        .New()
        .Https(4433)
        .HttpsCertificate("<path to .pfx file>")
        .Build();
```
To request a certificate via Let's Encrypt you can use the nuget tool https://www.nuget.org/packages/LetsencryptCert .When correctly configured (see README of this tool), it will automatically request a new valid certificate before the old certificate will be invalid. Let's Encrypt will call your website via HTTP to check the domain, so HTTP has to be enabled. All you have to do in your web server besides configuring ```LetsencryptCert```:

```cs
var server =
    WebServer
        .New()
        .Http()
        .Https()
        .UseLetsEncrypt()
        .Build();
```
 
### Serving a web site from .NET resource <a name="websitefromresource"></a>
WebServerLight can host a complete web site and other resources from files which are included as .NET resources. You have to include the files in your .csproj project file like this:

```cs
<ItemGroup>
    <EmbeddedResource Include="./website/index.html">
      <LogicalName>/index.html</LogicalName>
    </EmbeddedResource>
    <EmbeddedResource Include="./website/css/styles.css">
      <LogicalName>/css/styles.css</LogicalName>
    </EmbeddedResource>
    <EmbeddedResource Include="./website/scripts/script.js">
      <LogicalName>/scripts/script.js</LogicalName>
    </EmbeddedResource>
    <EmbeddedResource Include="./website/images/image.jpg">
      <LogicalName>image</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

The ```LogicalName``` is at the same time the resource part of your URL. ```/index.html``` does not have to be specified.

To activate this you have to call ```WebsiteFromResource()```:

```cs
var server =
    WebServer
        .New()
        .Http()
        .WebsiteFromResource()
        .Build();
```

### Use range requests for streaming media files like videos <a name="userange"></a>
If you want to stream video or audio files, all you have to do is activating it with the help of the method ```UseRange()```:

```cs
var server =
    WebServer
        .New()
        .Http()
        .WebsiteFromResource()
        .UseRange()
        .Build();
```
When the web site requests a range request, the media file will be streamed.

## Routing <a name="routing"></a>

WebServerLight has a routing concept. You can add new routes with the help of the function ```Route```:
```cs
var server =
    WebServer
        .Route()
```
Threre are several route-Objects you can include:
* HttpRoute (only called when using http:// scheme)
* HttpsRoute (only called when using https:// scheme)
* MethodRoute (only called when the HTTP method is one of GET, PUT, POST, ...)
* PathRoute (only called when the URL path part starts with the as parameter given path)
* PathExactRoute (only called when the URL path part equals the as parameter given path)

These routes are included with the help of the ```New()``` static function like this:

```cs
var server =
    WebServer
        .Route(MethodRoute
            .New(Method.Get))
```
Here you add a new route that is only called when the HTTP method is GET.

The routes can be combined like this:

```cs
var server =
    WebServer
        .Route(MethodRoute
            .New(Method.Get))
                .Add(PathRoute
                        .New("/image")
                        .Request(GetImage))
                .Add(PathRoute
                        .New("/video")
                        .Request(GetVideo)))
```
When the method is GET and when the path starts with ```/image```, then the request ```GetImage``` is being called. Otherwise when the method is GET and when the path starts with ```/video```, then the request ```GetImage``` is being called.

## Serving requests <a name="servingrequests"></a>
Like you have seen in the previous section, a request is being served with the call to ```Request```. ```Request``` has the following signature:

```cs
public Route Request(Func<IRequest, Task<bool>> request);
```

You include a request function as parameter which is called when all routing conditions are OK for the specific HTTP request. The routing function has a parameter of type ```IRequest``` and retuns asyncronously a boolean result. If it is true, the request is being served. If it is false, the next route is being probed.

### Returning a string response

Use the method ```IRequest.SendTextAsync``` to send a string response.

### Returning a stream response

Use the method ```IRequest.SendAsync``` to send a stream response like in the request method ```GetImage```:

```cs
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
```

### Serving a JSON request

Here is an example for responding to a JSON POST request:

```cs
var server =
    WebServer
        .New()
        .Http()
        .Route(MethodRoute
                .New(Method.Post)
                .Add(PathRoute
                        .New("/json/cmd")
                        .Request(JsonPost)));
```
with the method ```JsonPost```:

```cs
async Task<bool> JsonPost(IRequest request)
{
    var data = await request.DeserializeAsync<Data>();
    var response = new Response([
        new Contact("Charlie Parker", 34),
        new Contact("Miles Davis", 90),
        new Contact("John Coltrane", 99)], 123, request.Url);

    await request.SendJsonAsync(response);
    return true;
}
```

## WebSockets <a name="websockets"></a>
...
