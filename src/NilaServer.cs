using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Nila;

public class NilaServer
{
    private TcpListener? _listener;
    private Func<HttpContext, CancellationToken, Task>? _handler;
    private CancellationTokenSource? _cts;

    public void Start(ServerOptions options)
    {
        _listener = new TcpListener(IPAddress.Loopback, options.Port);
        _listener.Start();

        _cts = new CancellationTokenSource();
        _ = AcceptConnectionsAsync(_cts.Token);
    }

    public void ProcessAsync(Func<HttpContext, CancellationToken, Task> handler)
    {
        _handler = handler;
    }

    public void Stop()
    {
        Debug.Assert(_listener is not null);
        Debug.Assert(_cts is not null);

        _cts.Cancel();
        _listener.Stop();
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        Debug.Assert(_listener is not null);

        while (!ct.IsCancellationRequested)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync(ct);
            _ = HandleConnectionAsync(tcpClient, ct);
        }
    }

    private async Task HandleConnectionAsync(TcpClient tcpClient, CancellationToken ct)
    {
        Debug.Assert(_handler is not null);

        var stream = tcpClient.GetStream();
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream);

        var request = new HttpRequest(reader);
        await request.ParseAsync(ct);

        var response = new HttpResponse(writer, request.Protocol);
        var context = new HttpContext(request, response);

        await _handler.Invoke(context, ct);

        await response.FlushAsync(ct);
        tcpClient.Close();
    }
}