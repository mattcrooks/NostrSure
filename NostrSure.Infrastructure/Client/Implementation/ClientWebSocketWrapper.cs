using System.Net.WebSockets;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Wrapper for ClientWebSocket to implement IClientWebSocket
/// </summary>
public sealed class ClientWebSocketWrapper : IClientWebSocket
{
    private readonly ClientWebSocket _webSocket;

    public ClientWebSocketWrapper(ClientWebSocket webSocket)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
    }

    public WebSocketState State => _webSocket.State;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        return _webSocket.ConnectAsync(uri, cancellationToken);
    }

    public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public void Dispose()
    {
        _webSocket.Dispose();
    }
}