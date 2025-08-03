using System.Net.WebSockets;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Manages WebSocket connection lifecycle operations
/// </summary>
public interface IConnectionManager : IDisposable
{
    /// <summary>
    /// Current WebSocket state
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    /// Establishes a connection to the specified URI
    /// </summary>
    /// <param name="uri">WebSocket URI to connect to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the connection operation</returns>
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the WebSocket connection
    /// </summary>
    /// <param name="closeStatus">Close status code</param>
    /// <param name="statusDescription">Optional close description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the close operation</returns>
    Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                    string? statusDescription = null,
                    CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the connection is disconnected
    /// </summary>
    event EventHandler? Disconnected;
}