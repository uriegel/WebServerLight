namespace WebServerLight;

class PayloadStream(Memory<byte> payloadBegin, Stream networkStream, int length) : Streambase
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var pos = Math.Min(buffer.Length, payloadBegin.Length);
        var copyBuffer = payloadBegin[..pos];
        copyBuffer.CopyTo(buffer);


        var test = System.Text.Encoding.UTF8.GetString(buffer[..pos].Span);

        payloadBegin = payloadBegin[pos..];
        return ValueTask.FromResult(pos);
    }
}