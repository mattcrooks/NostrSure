using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Implementation;
using System.Text;

namespace NostrSure.Tests.Client.Connection;

[TestCategory("Client")]
[TestCategory("Connection")]
[TestClass]
public class ConnectionStateManagerTests
{
    private ConnectionStateManager _stateManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _stateManager = new ConnectionStateManager();
    }

    [TestMethod]
    public void CurrentState_InitiallyNone()
    {
        // Assert
        Assert.AreEqual(System.Net.WebSockets.WebSocketState.None, _stateManager.CurrentState);
        Assert.IsFalse(_stateManager.IsConnected);
    }

    [TestMethod]
    public void UpdateState_ChangesCurrentState()
    {
        // Arrange
        var newState = System.Net.WebSockets.WebSocketState.Open;

        // Act
        _stateManager.UpdateState(newState);

        // Assert
        Assert.AreEqual(newState, _stateManager.CurrentState);
        Assert.IsTrue(_stateManager.IsConnected);
    }

    [TestMethod]
    public void UpdateState_FiresStateChangedEvent()
    {
        // Arrange
        var eventFired = false;
        var receivedState = System.Net.WebSockets.WebSocketState.None;
        
        _stateManager.StateChanged += (sender, state) =>
        {
            eventFired = true;
            receivedState = state;
        };

        // Act
        _stateManager.UpdateState(System.Net.WebSockets.WebSocketState.Open);

        // Assert
        Assert.IsTrue(eventFired);
        Assert.AreEqual(System.Net.WebSockets.WebSocketState.Open, receivedState);
    }

    [TestMethod]
    public void UpdateState_SameState_DoesNotFireEvent()
    {
        // Arrange
        _stateManager.UpdateState(System.Net.WebSockets.WebSocketState.Open);
        var eventFired = false;
        
        _stateManager.StateChanged += (sender, state) =>
        {
            eventFired = true;
        };

        // Act
        _stateManager.UpdateState(System.Net.WebSockets.WebSocketState.Open);

        // Assert
        Assert.IsFalse(eventFired);
    }

    [TestMethod]
    public void IsConnected_ReturnsCorrectValue()
    {
        // Test initial state
        Assert.IsFalse(_stateManager.IsConnected);

        // Test connected state
        _stateManager.UpdateState(System.Net.WebSockets.WebSocketState.Open);
        Assert.IsTrue(_stateManager.IsConnected);

        // Test disconnected state
        _stateManager.UpdateState(System.Net.WebSockets.WebSocketState.Closed);
        Assert.IsFalse(_stateManager.IsConnected);
    }
}