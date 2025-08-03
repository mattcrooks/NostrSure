using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Reflection;

namespace NostrSure.Tests.Client.Connection;

[TestClass]

[TestCategory("Connection - MessageSender")]
public class MessageSenderTests
{
    private ClientWebSocket _webSocket = null!;
    private ConnectionErrorHandler _errorHandler = null!;
    private ConnectionStateManager _stateManager = null!;
    private MessageSender _sender = null!;

    [TestInitialize]
    public void Setup()
    {
        _webSocket = new ClientWebSocket();
        _errorHandler = new ConnectionErrorHandler();
        _stateManager = new ConnectionStateManager();
        _sender = new MessageSender(_webSocket, _errorHandler, _stateManager);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SendAsync_ThrowsIfNotConnected()
    {
        await _sender.SendAsync("test");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SendAsync_ThrowsIfMessageNullOrEmpty()
    {
        _stateManager.UpdateState(WebSocketState.Open);
        var stateField = typeof(ClientWebSocket).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        if (stateField != null) stateField.SetValue(_webSocket, WebSocketState.Open);
        await _sender.SendAsync("");
    }
}
