using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NostrSure.Tests.Client.Connection;

[TestClass]

[TestCategory("Connection")]
public class WebSocketConnectionTests_Coverage
{
    private class DummyManager : IConnectionManager
    {
        public WebSocketState State => WebSocketState.Open;
        public event EventHandler? Disconnected;
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? statusDescription = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public void RaiseDisconnected(object? sender, EventArgs args) => Disconnected?.Invoke(sender, args);
    }
    private class DummyReceiver : IMessageReceiver
    {
        public event EventHandler<string>? MessageReceived;
        public Task<string> ReceiveAsync(CancellationToken cancellationToken = default) => Task.FromResult("msg");
        public Task StartReceivingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopReceivingAsync() => Task.CompletedTask;
        public void Dispose() { }
        public void RaiseMessageReceived(object? sender, string msg) => MessageReceived?.Invoke(sender, msg);
    }
    private class DummySender : IMessageSender
    {
        public Task SendAsync(string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private class DummyErrorHandler : IConnectionErrorHandler
    {
        public event EventHandler<Exception>? ErrorOccurred;
        public Task HandleErrorAsync(Exception exception, string context) => Task.CompletedTask;
        public bool ShouldReconnect(Exception exception) => false;
        public void RaiseErrorOccurred(object? sender, Exception ex) => ErrorOccurred?.Invoke(sender, ex);
    }
    private class DummyStateManager : IConnectionStateManager
    {
        public WebSocketState CurrentState => WebSocketState.Open;
        public bool IsConnected => true;
        public event EventHandler<WebSocketState>? StateChanged;
        public void UpdateState(WebSocketState newState) { }
    }

    [TestMethod]
    public async Task DelegatesEventsAndMethods()
    {
        var conn = new WebSocketConnection(
            new DummyManager(),
            new DummyReceiver(),
            new DummySender(),
            new DummyErrorHandler(),
            new DummyStateManager(),
            null);
        bool msgFired = false, errFired = false, discFired = false;
        conn.MessageReceived += (s, m) => msgFired = true;
        conn.ErrorOccurred += (s, e) => errFired = true;
        conn.Disconnected += (s, e) => discFired = true;
        await conn.ConnectAsync(new Uri("wss://relay.damus.io"));
        await conn.SendAsync("hi");
        var msg = await conn.ReceiveAsync();
        await conn.CloseAsync();
        conn.Dispose();
        // Simulate events using public methods
        var receiver = (DummyReceiver)conn.GetType().GetField("_messageReceiver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(conn)!;
        receiver.RaiseMessageReceived(conn, "msg");
        var errorHandler = (DummyErrorHandler)conn.GetType().GetField("_errorHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(conn)!;
        errorHandler.RaiseErrorOccurred(conn, new Exception());
        var manager = (DummyManager)conn.GetType().GetField("_connectionManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(conn)!;
        manager.RaiseDisconnected(conn, EventArgs.Empty);
        Assert.IsTrue(msgFired);
        Assert.IsTrue(errFired);
        Assert.IsTrue(discFired);
        Assert.AreEqual(WebSocketState.Open, conn.State);
        Assert.AreEqual("msg", msg);
    }
}
