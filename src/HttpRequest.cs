namespace Nila;

public class HttpRequest
{
    public string Method { get; private set; } = default!;
    public string Path { get; private set; } = default!;
    public string Protocol { get; private set; } = default!;
    public readonly Dictionary<string, string> Headers = [];

    private readonly StreamReader _reader;

    public HttpRequest(StreamReader reader)
    {
        _reader = reader;
    }

    internal async Task ParseAsync(CancellationToken ct)
    {
        // https://datatracker.ietf.org/doc/html/rfc2616#section-5.1
        // Request-Line   = Method SP Request-URI SP HTTP-Version CRLF
        var requestLine = await _reader.ReadLineAsync(ct);
        if (requestLine is null)
        {
            //TODO: Handle this case
            return;
        }

        var requestParts = requestLine.Split(" ");
        Method = requestParts[0];
        Path = requestParts[1];
        Protocol = requestParts[2];

        while (true)
        {
            var headerLine = await _reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                break;
            }
            
            var separatorIndex = headerLine.IndexOf(':');
            if(separatorIndex is -1)
            {
                continue;
            }

            var headerKey = headerLine[..separatorIndex].Trim();
            var headerVal = headerLine[(separatorIndex+1)..].Trim();

            Headers.Add(headerKey, headerVal);
        }
    }
}