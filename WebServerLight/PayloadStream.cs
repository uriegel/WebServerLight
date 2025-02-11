using CsTools;

namespace WebServerLight;

class PayloadStream(Memory<byte> payloadBegin, Stream networkStream, int length) : StreamBase
{
    public override bool CanRead => true;

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var pos = Math.Min(buffer.Length, payloadBegin.Length);
        var copyBuffer = payloadBegin[..pos];
        copyBuffer.CopyTo(buffer);
        payloadBegin = payloadBegin[pos..];
        return ValueTask.FromResult(pos);
    }
}