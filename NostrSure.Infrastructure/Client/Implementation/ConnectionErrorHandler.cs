using System.Net.WebSockets;
using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Centralized error handling for WebSocket connections
/// </summary>
public sealed class ConnectionErrorHandler : IConnectionErrorHandler
{
    private readonly ILogger<ConnectionErrorHandler>? _logger;

    public ConnectionErrorHandler(ILogger<ConnectionErrorHandler>? logger = null)
    {
        _logger = logger;
    }

    public event EventHandler<Exception>? ErrorOccurred;

    public async Task HandleErrorAsync(Exception exception, string context)
    {
        _logger?.LogError(exception, "WebSocket error in {Context}: {Message}", context, exception.Message);
        
        // Fire error event
        ErrorOccurred?.Invoke(this, exception);
        
        // Allow for async error handling if needed
        await Task.CompletedTask;
    }

    public bool ShouldReconnect(Exception exception)
    {
        return exception switch
        {
            // Network-related exceptions that warrant retry
            HttpRequestException => true,
            SocketException => true,
            TimeoutException => true,
            
            // WebSocket-specific exceptions
            WebSocketException wsEx => wsEx.WebSocketErrorCode switch
            {
                WebSocketError.ConnectionClosedPrematurely => true,
                WebSocketError.Faulted => true,
                WebSocketError.HeaderError => false, // Don't retry on header errors
                WebSocketError.InvalidMessageType => false, // Don't retry on invalid message types
                WebSocketError.InvalidState => false, // Don't retry on invalid state
                WebSocketError.NativeError => true,
                WebSocketError.NotAWebSocket => false, // Don't retry if not a WebSocket
                WebSocketError.Success => false, // This shouldn't be an error
                WebSocketError.UnsupportedProtocol => false, // Don't retry on protocol mismatch
                WebSocketError.UnsupportedVersion => false, // Don't retry on version mismatch
                _ => true // Default to retry for unknown WebSocket errors
            },
            
            // Operation cancelled - usually intentional, don't retry
            OperationCanceledException => false,
            
            // Invalid operations - usually programming errors, don't retry
            InvalidOperationException => false,
            ArgumentException => false,
            
            // Default to retry for unknown exceptions
            _ => true
        };
    }
}