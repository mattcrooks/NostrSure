using System.Net.WebSockets;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Abstraction for WebSocket connection management
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task SendAsync(string message, CancellationToken cancellationToken = default);
    Task<string> ReceiveAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                    string? statusDescription = null,
                    CancellationToken cancellationToken = default);
    event EventHandler<string>? MessageReceived;
    event EventHandler<Exception>? ErrorOccurred;
    event EventHandler? Disconnected;
}

/// <summary>
/// Factory for creating WebSocket connections
/// </summary>
public interface IWebSocketFactory
{
    IWebSocketConnection Create();
}