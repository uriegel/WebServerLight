# WebServerLight
A C# light weight web server

## Setup

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

## Configuration
The Web Server can be configured with the help of many configuration functions

### Enabling HTTP and/or HTTPS

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
### Certificate for HTTPS

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
 