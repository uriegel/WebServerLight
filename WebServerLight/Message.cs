using System.Text;

using static System.Console;

namespace WebServerLight;

class Message
{
    public static async Task<Message?> Read(Server server, Stream networkStream, CancellationToken cancellation)
    {
        using var reader = new StreamReader(networkStream, Encoding.UTF8, leaveOpen: true, bufferSize: 8192);
        var request = await reader.ReadLineAsync();

        var headers = GetHeaders(reader).ToDictionary();
        WriteLine(headers);

        // TODO check POST request, read binary

        return null;
    }

    static IEnumerable<KeyValuePair<string, string>> GetHeaders(StreamReader reader)
    {
        while (true)
        {
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
                break;
            var parts = headerLine.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            yield return new(parts[0], parts[1]);
        }
    }
}