using System.Net.Sockets;
using System.Text;

namespace Nila.GoldenTests;

public class GoldenTests : IDisposable
{
    private readonly NilaServer _server;

    public GoldenTests()
    {
        _server = new NilaServer();
        _server.ProcessAsync(async (ctx, ct) =>
        {
            var method = ctx.Request.Method;
            var path = ctx.Request.Path;
            if (method == "GET" && path == "/")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ReasonPhrase = "OK";
            }
            else if (method == "GET" && path == "/content-length")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ReasonPhrase = "OK";
                ctx.Response.Headers["Content-Length"] = "10";

                // A 20 character response. Server should only send 10 chars. 'char' -> 1byte
                await ctx.Response.WriteBodyAsync("0123456789abcdefghij", ct);
            }
            else if (method == "GET" && path == "/custom-header")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ReasonPhrase = "OK";

                var customHeaderValue = ctx.Request.Headers.GetValueOrDefault("Custom-Header", string.Empty);

                ctx.Response.Headers.Add("Content-Type", "text/plane");
                ctx.Response.Headers.Add("Content-Length", customHeaderValue.Length.ToString());
                await ctx.Response.WriteBodyAsync(customHeaderValue, ct);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Headers["Content-Type"] = "text/plain";
                ctx.Response.ReasonPhrase = "Not Found";
                ctx.Response.Headers["Content-Length"] = "9";
                await ctx.Response.WriteBodyAsync("Not Found", ct);
            }
        });

        _server.Start(new() { Port = 2603 });
    }

    [Fact]
    public async Task TestGetRoot_Returns200()
    {
        var request = """
            GET / HTTP/1.1
            Host: localhost:2603
            Connection: close
            """;

        var response = await SendTcpRequest(request);

        var expected = """
            HTTP/1.1 200 OK
            \r\n
            """;

        AssertEqual(expected, response);
    }

    [Fact]
    public async Task TestGetContentLength_LimitBodyToContentLength()
    {
        var request = """
            GET /content-length HTTP/1.1
            Host: localhost:2603
            Connection: close
            """;

        var response = await SendTcpRequest(request);

        var expected = """
            HTTP/1.1 200 OK
            Content-Length: 10
            
            0123456789
            """;

        AssertEqual(expected, response);
    }

    [Fact]
    public async Task TestGetCustomHeader_ReturnsCustomHeader()
    {
        var headerValue = Guid.NewGuid().ToString();

        var request = $"""
            GET /custom-header HTTP/1.1
            Host: localhost:2603
            Custom-Header: {headerValue}
            Connection: close

            
            """;

        var response = await SendTcpRequest(request);

        var expected = $"""
            HTTP/1.1 200 OK
            Content-Type: text/plane
            Content-Length: {headerValue.Length}

            {headerValue}
            """;

        AssertEqual(expected, response);
    }

    [Fact]
    public async Task TestGetEchoBody_ReturnsRequestBody()
    {
        var body = Guid.NewGuid().ToString();

        var request = $"""
            GET /echo-body HTTP/1.1
            Host: localhost:2603
            Content-Type: text/plane
            Content-Length: {body.Length}
            Connection: close

            {body} 
            """;

        var response = await SendTcpRequest(request);

        var expected = $"""
            HTTP/1.1 200 OK
            Content-Type: text/plane
            Content-Length: {body.Length}

            {body}
            """;

        AssertEqual(expected, response);
    }

    [Fact]
    public async Task TestConcurrentRequests_AllReturns200()
    {
        var concurrency = 50;

        var request = """
            GET / HTTP/1.1
            Host: localhost:2603
            Connection: close
            """;

        var expected = """
            HTTP/1.1 200 OK 
            """;

        var requests = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(() => SendTcpRequest(request)));

        var results = await Task.WhenAll(requests);

        foreach (var result in results)
            AssertEqual(expected, result);
    }

    [Fact]
    public async Task TestInvalidPath_Returns404()
    {
        var request = """
            GET /notfound HTTP/1.1
            Host: localhost:2603
            Connection: close

            """;

        var response = await SendTcpRequest(request);

        var expected = """
            HTTP/1.1 404 Not Found
            Content-Type: text/plain
            Content-Length: 9
            
            Not Found
            """;

        AssertEqual(expected, response);
    }

    private async Task<string> SendTcpRequest(string request)
    {
        // Normalize request.
        request = request.Replace("\n", "\r\n").Replace(@"\r\n", "\r\n");
        using var client = new TcpClient("localhost", 2603);
        using var stream = client.GetStream();

        var bytes = Encoding.UTF8.GetBytes(request);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public void Dispose()
    {
        _server.Stop();
    }

    private void AssertEqual(string expected, string response)
    {
        var normalizedExpected = expected.Replace("\n", "\r\n").Replace(@"\r\n", "\r\n");
        Assert.Equal(normalizedExpected, response);
    }
}
