
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlassScreenMac.Services;

public sealed class YouTubePlayerHost : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _shutdown = new();

    private Task? _serverTask;
    private bool _disposed;

    public Uri BaseAddress { get; private set; } = null!;

    public void Start()
    {
        if (_serverTask != null)
            return;

        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();

        int port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();

        BaseAddress = new Uri($"http://127.0.0.1:{port}/");

        _listener.Prefixes.Add(BaseAddress.ToString());
        _listener.Start();

        _serverTask = Task.Run(ServeAsync);
    }

    public Uri GetPlayerUri(string videoId)
    {
        // Validate the ID before constructing the URL.
        _ = YouTubePlayerService.BuildPlayerHtml(videoId);

        return new Uri(
            BaseAddress,
            $"player/{Uri.EscapeDataString(videoId)}");
    }

    private async Task ServeAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await HandleRequestAsync(context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"YouTube player host error: {ex.Message}");
            }
            finally
            {
                context.Response.Close();
            }
        }
    }

    private static async Task HandleRequestAsync(
        HttpListenerContext context)
    {
        var response = context.Response;

        response.Headers["Cache-Control"] = "no-store";
        response.Headers["X-Content-Type-Options"] = "nosniff";

        var path = context.Request.Url?.AbsolutePath ?? "";

        if (context.Request.HttpMethod != "GET" ||
            !path.StartsWith("/player/", StringComparison.Ordinal))
        {
            response.StatusCode = 404;
            return;
        }

        string videoId = path["/player/".Length..];

        string html;

        try
        {
            html = YouTubePlayerService.BuildPlayerHtml(videoId);
        }
        catch (ArgumentException)
        {
            response.StatusCode = 400;
            return;
        }

        response.StatusCode = 200;
        response.ContentType = "text/html; charset=utf-8";
        response.Headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";

        var bytes = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = bytes.Length;

        await response.OutputStream.WriteAsync(bytes);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _shutdown.Cancel();
        _listener.Close();
        _shutdown.Dispose();
    }
}
