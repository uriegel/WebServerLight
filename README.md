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
    <PackageReference Include="WebServerLight" Version="0.0.23-beta-23" />
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
...
## Serving requests <a name="servingrequests"></a>
...
## WebSockets <a name="websockets"></a>
...
