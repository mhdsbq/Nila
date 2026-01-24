namespace Nila;

public class HttpRequest
{
    public string Method { get; private set; } = default!;
    public string Path { get; private set; } = default!;
    public string Protocol { get; private set; } = default!;

    private readonly StreamReader Reader;

    public HttpRequest(StreamReader reader)
    {
        Reader = reader;        
    }

    internal async Task ParseAsync(CancellationToken ct)
    {
        // https://datatracker.ietf.org/doc/html/rfc2616#section-5.1
        // Request-Line   = Method SP Request-URI SP HTTP-Version CRLF
        var requestLine = await Reader.ReadLineAsync(ct);
        if (requestLine is null)
        {
            //TODO: Handle this case
            return;
        }

        var requestParts = requestLine.Split(" ");
        Method = requestParts[0];
        Path = requestParts[1];
        Protocol = requestParts[2];
    }
}