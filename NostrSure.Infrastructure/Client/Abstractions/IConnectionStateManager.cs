using System.Net.WebSockets;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Manages WebSocket connection state tracking and notifications
/// </summary>
public interface IConnectionStateManager
{
    /// <summary>
    /// Current WebSocket state
    /// </summary>
    WebSocketState CurrentState { get; }
    
    /// <summary>
    /// Indicates if the connection is currently active
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Event raised when the connection state changes
    /// </summary>
    event EventHandler<WebSocketState>? StateChanged;
    
    /// <summary>
    /// Updates the current connection state
    /// </summary>
    /// <param name="newState">The new WebSocket state</param>
    void UpdateState(WebSocketState newState);
}