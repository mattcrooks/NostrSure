using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;
using System.Text;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Factory for creating refactored WebSocket connections with proper dependency injection
/// </summary>
public class RefactoredWebSocketFactory : IWebSocketFactory
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public RefactoredWebSocketFactory(
        ILoggerFactory? loggerFactory,
        ObjectPool<StringBuilder> stringBuilderPool)
    {
        _loggerFactory = loggerFactory;
        _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
    }

    public IWebSocketConnection Create()
    {
        // Create shared WebSocket instance
        var webSocket = new ClientWebSocket();

        // Create component implementations
        var stateManager = new ConnectionStateManager(_loggerFactory?.CreateLogger<ConnectionStateManager>());
        var errorHandler = new ConnectionErrorHandler(_loggerFactory?.CreateLogger<ConnectionErrorHandler>());
        var connectionManager = new ConnectionManager(stateManager, errorHandler, _loggerFactory?.CreateLogger<ConnectionManager>());
        var messageReceiver = new MessageReceiver(webSocket, errorHandler, stateManager, _stringBuilderPool, _loggerFactory?.CreateLogger<MessageReceiver>());
        var messageSender = new MessageSender(webSocket, errorHandler, _loggerFactory?.CreateLogger<MessageSender>());

        // Create façade
        return new RefactoredWebSocketConnection(
            connectionManager,
            messageReceiver,
            messageSender,
            errorHandler,
            stateManager,
            _loggerFactory?.CreateLogger<RefactoredWebSocketConnection>());
    }
}