using System.Net.WebSockets;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Abstraction for ClientWebSocket to enable testing
/// </summary>
public interface IClientWebSocket : IDisposable
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}