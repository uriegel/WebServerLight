using CsTools;

namespace WebServerLight;

class PayloadStream(Memory<byte> payloadBegin, Stream networkStream, int length) : StreamBase
{
    public override bool CanRead => true;

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
    {
        var pos = Math.Min(buffer.Length, payloadBegin.Length);
        var copyBuffer = payloadBegin[..pos];
        copyBuffer.CopyTo(buffer);
        payloadRead += pos;
        payloadBegin = payloadBegin[pos..];
        if (pos == 0 && payloadRead == length)
            return pos;

        if (pos == 0 && payloadRead < length)
        {
            var read = await networkStream.ReadAsync(buffer, cancellation);
            payloadRead += read;
            return read;
        }
        else 
            return pos;
    }

    int payloadRead;    
}