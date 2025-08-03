using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Implementation;
using System.Text;
using System.Net.WebSockets;

namespace NostrSure.Tests.Client.Connection;

[TestCategory("Client")]
[TestCategory("Connection")]
[TestClass]
public class RefactoredWebSocketConnectionTests
{
    private RefactoredWebSocketFactory _factory = null!;
    private ILoggerFactory _loggerFactory = null!;
    private ObjectPool<StringBuilder> _stringBuilderPool = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var objectPoolProvider = new DefaultObjectPoolProvider();
        var policy = new StringBuilderPooledObjectPolicy();
        _stringBuilderPool = objectPoolProvider.Create(policy);
        
        _factory = new RefactoredWebSocketFactory(_loggerFactory, _stringBuilderPool);
    }

    [TestMethod]
    public void Create_ReturnsRefactoredWebSocketConnection()
    {
        // Act
        var connection = _factory.Create();

        // Assert
        Assert.IsNotNull(connection);
        Assert.IsInstanceOfType<RefactoredWebSocketConnection>(connection);
        Assert.AreEqual(WebSocketState.None, connection.State);
    }

    [TestMethod]
    public void ConnectionStateManager_InitialState_IsNone()
    {
        // Arrange
        var stateManager = new ConnectionStateManager();

        // Assert
        Assert.AreEqual(WebSocketState.None, stateManager.CurrentState);
        Assert.IsFalse(stateManager.IsConnected);
    }

    [TestMethod]
    public void ConnectionStateManager_UpdateToOpen_IsConnected()
    {
        // Arrange
        var stateManager = new ConnectionStateManager();

        // Act
        stateManager.UpdateState(WebSocketState.Open);

        // Assert
        Assert.AreEqual(WebSocketState.Open, stateManager.CurrentState);
        Assert.IsTrue(stateManager.IsConnected);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _loggerFactory?.Dispose();
    }
}