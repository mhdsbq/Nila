namespace Nila;

public class HttpResponse
{
    public string Protocol { get; private set; }
    public int StatusCode { get; set; } = 200;
    public string ReasonPhrase { get; set; } = "OK";
    public Dictionary<string, string> Headers = [];

    private readonly StreamWriter _writer;
    private bool _isFlushed = false;

    public HttpResponse(StreamWriter writer, string protocol)
    {
        _writer = writer;
        Protocol = protocol;
    }

    /// <summary>
    /// Flushes status line and headers to response stream
    /// </summary>
    internal async Task FlushAsync(CancellationToken ct)
    {
        // https://datatracker.ietf.org/doc/html/rfc2616#section-6.1
        // Status-Line = HTTP-Version SP Status-Code SP Reason-Phrase CRLF
        var statusLine = $"{Protocol} {StatusCode} {ReasonPhrase}\r\n";

        // TODO: Investigate write async not accepting cancellation token
        await _writer.WriteAsync(statusLine);
        await _writer.WriteAsync("\r\n");
        await _writer.FlushAsync(ct);
        
        _isFlushed = true;
    }
}