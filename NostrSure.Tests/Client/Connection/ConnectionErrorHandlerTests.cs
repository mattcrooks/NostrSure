using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;
using System.Net.WebSockets;
using System.Net.Sockets;

namespace NostrSure.Tests.Client.Connection;

[TestClass]

[TestCategory("Connection - ConnectionErrorHandler")]
public class ConnectionErrorHandlerTests
{
    [TestMethod]
    public async Task HandleErrorAsync_FiresEventAndLogs()
    {
        var handler = new ConnectionErrorHandler();
        bool fired = false;
        handler.ErrorOccurred += (s, e) => fired = true;
        await handler.HandleErrorAsync(new Exception("test"), "ctx");
        Assert.IsTrue(fired);
    }

    [TestMethod]
    public void ShouldReconnect_AllBranches()
    {
        var handler = new ConnectionErrorHandler();
        Assert.IsTrue(handler.ShouldReconnect(new HttpRequestException()));
        Assert.IsTrue(handler.ShouldReconnect(new SocketException()));
        Assert.IsTrue(handler.ShouldReconnect(new TimeoutException()));
        Assert.IsFalse(handler.ShouldReconnect(new OperationCanceledException()));
        Assert.IsFalse(handler.ShouldReconnect(new InvalidOperationException()));
        Assert.IsFalse(handler.ShouldReconnect(new ArgumentException()));
        var wsEx = new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
        Assert.IsTrue(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.HeaderError);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.InvalidMessageType);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.InvalidState);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.NativeError);
        Assert.IsTrue(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.NotAWebSocket);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.Success);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.UnsupportedProtocol);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        wsEx = new WebSocketException(WebSocketError.UnsupportedVersion);
        Assert.IsFalse(handler.ShouldReconnect(wsEx));
        Assert.IsTrue(handler.ShouldReconnect(new Exception()));
    }
}
