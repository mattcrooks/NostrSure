using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Handles WebSocket message transmission
/// </summary>
public sealed class MessageSender : IMessageSender
{
    private readonly ClientWebSocket _webSocket;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly IConnectionStateManager _stateManager;
    private readonly ILogger<MessageSender>? _logger;

    public MessageSender(
        ClientWebSocket webSocket,
        IConnectionErrorHandler errorHandler,
        IConnectionStateManager stateManager,
        ILogger<MessageSender>? logger = null)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger;
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        // Check actual WebSocket state as primary source of truth
        if (_webSocket.State != WebSocketState.Open)
        {
            _logger?.LogError("WebSocket send failed - WebSocket.State: {WebSocketState}, StateManager.IsConnected: {StateManagerConnected}", 
                             _webSocket.State, _stateManager.IsConnected);
            throw new InvalidOperationException($"WebSocket is not connected. Current state: {_webSocket.State}");
        }

        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        try
        {
            _logger?.LogDebug("Sending message: {MessageLength} characters", message.Length);
            
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
                
            _logger?.LogDebug("Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send message");
            await _errorHandler.HandleErrorAsync(ex, nameof(SendAsync));
            throw;
        }
    }
}