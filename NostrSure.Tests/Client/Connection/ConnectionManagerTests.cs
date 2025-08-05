using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NostrSure.Tests.Client.Connection;

[TestClass]

[TestCategory("Connection")]
public class ConnectionManagerTests
{
    private MockClientWebSocket _webSocket = null!;
    private ConnectionStateManager _stateManager = null!;
    private ConnectionErrorHandler _errorHandler = null!;
    private ConnectionManager _manager = null!;
    private bool _disconnectedFired;

    [TestInitialize]
    public void Setup()
    {
        _webSocket = new MockClientWebSocket();
        _stateManager = new ConnectionStateManager();
        _errorHandler = new ConnectionErrorHandler();
        _manager = new ConnectionManager(_webSocket, _stateManager, _errorHandler);
        _disconnectedFired = false;
        _manager.Disconnected += (s, e) => _disconnectedFired = true;
    }

    [TestMethod]
    public async Task ConnectAsync_UpdatesState()
    {
        await _manager.ConnectAsync(new Uri("wss://relay.example.com"));
        Assert.AreEqual(WebSocketState.Open, _manager.State);
        Assert.IsTrue(_stateManager.IsConnected);
    }

    [TestMethod]
    public async Task CloseAsync_UpdatesStateAndFiresEvent()
    {
        await _manager.ConnectAsync(new Uri("wss://relay.example.com"));
        await _manager.CloseAsync();
        Assert.AreNotEqual(WebSocketState.Open, _manager.State);
        Assert.IsTrue(_disconnectedFired);
    }

    [TestMethod]
    public void Dispose_CancelsAndDisposes()
    {
        _manager.Dispose();
    }

    // Mock implementation for testing
    private class MockClientWebSocket : IClientWebSocket
    {
        private WebSocketState _state = WebSocketState.None;

        public WebSocketState State => _state;

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            _state = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _state = WebSocketState.Closed;
        }
    }
}
