using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

using static System.Console;

namespace WebServerLight;

class SocketSession(Server server, TcpClient tcpClient, bool isSecured)
{
    public DateTime ConnectTime { get; } = DateTime.Now;
    public int Id { get; } = Interlocked.Increment(ref seedId);

    public TcpClient TcpClient { get => tcpClient; }

    public static async void StartReceiving(Server server, TcpClient tcpClient, bool isSecured)
    {
        var session = new SocketSession(server, tcpClient, isSecured);
        await session.Receive();
    }

    public async Task Receive()
    {
        try
        {
            while (true)
            {
                DateTime? startTime = null;
                if (networkStream == null)
                {
                    networkStream = isSecured ? await GetTlsNetworkStreamAsync(TcpClient) : TcpClient.GetStream();
                    startTime = ConnectTime;
                }

                if (shutdown)
                    break;
                var session = new RequestSession(server, this, networkStream!, startTime);
                if (!await session.StartAsync())
                    break;
            }
        }
        catch (AuthenticationException ae)
        {
            Error.WriteLine($"{Id}- An authentication error has occurred while reading socket, session: {tcpClient.Client.RemoteEndPoint as IPEndPoint}, error: {ae}");
        }
        catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
        {
            Error.WriteLine(() => $"{Id}- Closing socket session, reason: {e}");
            Close();
        }
        catch (Exception e) when (e is ObjectDisposedException)
        {
            Error.WriteLine($"{Id}- Object disposed");
            Close();
        }
        catch (Exception e)
        {
            Error.WriteLine($"{Id}- An error has occurred while reading socket, error: {e}");
        }
    }

    public void Close() => TcpClient.Close();

    public static void Shutdown() => shutdown = true;

    async Task<Stream?> GetTlsNetworkStreamAsync(TcpClient tcpClient)
    {
        var stream = tcpClient.GetStream();
        if (!isSecured)
            return null;

        // TODO 
        await Task.Delay(20000);

        var sslStream = new SslStream(stream);
        // if (server.Configuration.AllowRenegotiation)
        //     await sslStream.AuthenticateAsServerAsync(server.Configuration.Certificate!, false, server.Configuration.TlsProtocols, server.Configuration.CheckRevocation);
        // else
        //     await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions()
        //     {
        //         AllowRenegotiation = false,
        //         EnabledSslProtocols = server.Configuration.TlsProtocols,
        //         ServerCertificate = server.Configuration.Certificate,
        //         CertificateRevocationCheckMode = X509RevocationMode.Offline
        //     });

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
        WriteLine($"{Id}- Secure protocol: {sslStream.SslProtocol}, Cipher: {sslStream.CipherAlgorithm} strength {sslStream.CipherStrength}, Key exchange: {GetKeyExchangeAlgorithm(sslStream)} strength {sslStream.KeyExchangeStrength}, Hash: {GetHashAlgorithm(sslStream)} strength {sslStream.HashStrength}");

        return sslStream;
    }

    static int seedId;
    static bool shutdown;
    Stream? networkStream;
}