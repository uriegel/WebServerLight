using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using CsTools.Extensions;

using static System.Console;
using static CsTools.Functional.Memoization;

namespace WebServerLight.Sessions;

static class SecureSocketSession
{
    static Resetter Resetter { get; } = new Resetter();

    public static async Task<Stream?> GetTlsNetworkStreamAsync(this TcpClient tcpClient, int id, ServerBuilder configuration)
    {
        var stream = tcpClient.GetStream();
        var sslStream = new SslStream(stream);
        if (configuration.AllowRenegotiation)
            await sslStream.AuthenticateAsServerAsync(configuration.Certificate ?? GetCertificate(), false, SslProtocols.Tls12 | SslProtocols.Tls13, true);
        else
            await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions()
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ServerCertificate = configuration.Certificate ?? GetCertificate(),
                CertificateRevocationCheckMode = X509RevocationMode.Offline
            });

        string GetKeyExchangeAlgorithm(SslStream n) => (int)n.KeyExchangeAlgorithm == 44550 ? "ECDHE" : $"{n.KeyExchangeAlgorithm}";
        string GetHashAlgorithm(SslStream n)
        {
            return (int)n.HashAlgorithm switch
            {
                32781 => "SHA384",
                32780 => "SHA256",
                _ => $"{n.HashAlgorithm}",
            };
        }
        WriteLine($"{id}- Secure protocol: {sslStream.SslProtocol}, Cipher: {sslStream.CipherAlgorithm} strength {sslStream.CipherStrength}, Key exchange: {GetKeyExchangeAlgorithm(sslStream)} strength {sslStream.KeyExchangeStrength}, Hash: {GetHashAlgorithm(sslStream)} strength {sslStream.HashStrength}");

        return sslStream;
    }

    static Func<X509Certificate2> GetCertificate { get; } = Memoize(InitCertificate, Resetter);

    static Func<string> GetPfxPassword { get; } = Memoize(InitGetPfxPassword, Resetter);

    static X509Certificate2 InitCertificate()
    {
        var certFile = "LETS_ENCRYPT_DIR".GetEnvironmentVariable().AppendPath("certificate.pfx");
        var certificate = new X509Certificate2(certFile, GetPfxPassword());
        StartCertificateTimer();
        return certificate;
    }

    static string InitGetPfxPassword()
        => "/etc"
                .AppendPath("letsencrypt-uweb")
                ?.ReadAllTextFromFilePath()
                ?.Trim()
                ?? "".SideEffect(_ => WriteLine("!!!NO PASSWORD!!"));

    static void StartCertificateTimer()
        => certificateResetter ??= new(_ => Resetter.Reset(),
                null,
                TimeSpan.FromDays(1),
                TimeSpan.FromDays(1));
        
    static Timer? certificateResetter;
}