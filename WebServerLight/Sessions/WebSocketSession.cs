using System.IO.Compression;
using System.Text;
using System.Text.Json;
using WebServerLight.WebSockets;

namespace WebServerLight.Sessions;

record Extensions(bool PerMessageDeflate, bool WatchDog);

class WebSocketSession(string url, Stream networkStream, Extensions extensions) : IWebSocket
{
    public string Url { get => url; }
    public Task SendString(string payload)
    {
        var buffer = Encoding.UTF8.GetBytes(payload);
        var memStm = new MemoryStream(buffer, 0, buffer.Length, false, true);
        return WriteStreamAsync(memStm);
    }

    public Task SendJson<T>(T payload)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, payload, Json.Defaults);
        ms.Position = 0;
        return WriteStreamAsync(ms);
    }

    public void Close()
    {
        try
        {
            networkStream.Close();
        }
        catch { }
    }

    async Task WriteStreamAsync(MemoryStream payloadStream)
    {
        var (buffer, deflate) = GetPayload(payloadStream);
        var header = WriteHeader(buffer.Length, deflate);
        await semaphoreSlim.WaitAsync();
        try
        {
            networkStream.Write(header, 0, header.Length);
            networkStream.Write(buffer, 0, buffer.Length);
        }
        catch
        {
            try
            {
                networkStream.Close();
            }
            catch { }
            // TODO: error while sending events to a closed websocket!
            // throw;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }

    (byte[] buffer, bool deflate) GetPayload(MemoryStream payloadStream)
    {
        var deflate = extensions.PerMessageDeflate && payloadStream.Length > minSizeForDeflate;
        if (deflate)
        {
            var ms = new MemoryStream();
            var compressedStream = new DeflateStream(ms, CompressionMode.Compress, true);
            payloadStream.CopyTo(compressedStream);
            compressedStream.Close();
            ms.WriteByte(0); // BFinal!
            payloadStream = ms;
        }

        payloadStream.Capacity = (int)payloadStream.Length;
        return (payloadStream.GetBuffer(), deflate);
    }

    /// <summary>
    /// Schreibt den WebSocketHeader
    /// </summary>
    /// <param name="payloadLength"></param>
    /// <param name="deflate"></param>
    /// <param name="opcode"></param>
    byte[] WriteHeader(int payloadLength, bool deflate, OpCode? opcode = null)
    {
        if (opcode == null)
            opcode = OpCode.Text;
        var length = payloadLength;
        var FRRROPCODE = (byte)((deflate ? 0xC0 : 0x80) + (byte)(int)opcode.Value); //'FIN is set, and OPCODE is 1 (Text) or opCode

        int headerLength;
        if (length <= 125)
            headerLength = 2;
        else if (length <= ushort.MaxValue)
            headerLength = 4;
        else
            headerLength = 10;
        var buffer = new byte[headerLength];
        if (length <= 125)
        {
            buffer[0] = FRRROPCODE;
            buffer[1] = Convert.ToByte(length);
        }
        else if (length <= ushort.MaxValue)
        {
            buffer[0] = FRRROPCODE;
            buffer[1] = 126;
            var sl = (ushort)length;
            var byteArray = BitConverter.GetBytes(sl);
            var eins = byteArray[0];
            buffer[2] = byteArray[1];
            buffer[3] = eins;
        }
        else
        {
            buffer[0] = FRRROPCODE;
            buffer[1] = 127;
            var byteArray = BitConverter.GetBytes((ulong)length);
            var eins = byteArray[0];
            var zwei = byteArray[1];
            var drei = byteArray[2];
            var vier = byteArray[3];
            var fünf = byteArray[4];
            var sechs = byteArray[5];
            var sieben = byteArray[6];
            buffer[2] = byteArray[7];
            buffer[3] = sieben;
            buffer[4] = sechs;
            buffer[5] = fünf;
            buffer[6] = vier;
            buffer[7] = drei;
            buffer[8] = zwei;
            buffer[9] = eins;
        }
        return buffer;
    }

    readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    readonly int minSizeForDeflate = 10_000;
}
