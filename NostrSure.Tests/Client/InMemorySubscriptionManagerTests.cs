using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;

namespace NostrSure.Tests.Client;

[TestCategory("Client")]
[TestClass]
public class InMemorySubscriptionManagerTests
{
    private InMemorySubscriptionManager _manager = null!;

    [TestInitialize]
    public void Setup()
    {
        _manager = new InMemorySubscriptionManager();
    }

    [TestMethod]
    public void NewSubscriptionId_ReturnsUniqueIds()
    {
        // Act
        var id1 = _manager.NewSubscriptionId();
        var id2 = _manager.NewSubscriptionId();

        // Assert
        Assert.IsNotNull(id1);
        Assert.IsNotNull(id2);
        Assert.AreNotEqual(id1, id2);
        Assert.IsTrue(id1.StartsWith("sub_"));
        Assert.IsTrue(id2.StartsWith("sub_"));
    }

    [TestMethod]
    public void AddSubscription_AddsSubscriptionSuccessfully()
    {
        // Arrange
        var subscriptionId = "test_sub_1";

        // Act
        _manager.AddSubscription(subscriptionId);

        // Assert
        Assert.IsTrue(_manager.HasSubscription(subscriptionId));
    }

    [TestMethod]
    public void RemoveSubscription_RemovesSubscriptionSuccessfully()
    {
        // Arrange
        var subscriptionId = "test_sub_1";
        _manager.AddSubscription(subscriptionId);

        // Act
        _manager.RemoveSubscription(subscriptionId);

        // Assert
        Assert.IsFalse(_manager.HasSubscription(subscriptionId));
    }

    [TestMethod]
    public void HasSubscription_ReturnsFalseForNonExistentSubscription()
    {
        // Act & Assert
        Assert.IsFalse(_manager.HasSubscription("non_existent"));
    }

    [TestMethod]
    public void HasSubscription_ReturnsFalseForNullOrEmptyId()
    {
        // Act & Assert
        Assert.IsFalse(_manager.HasSubscription(null!));
        Assert.IsFalse(_manager.HasSubscription(""));
        Assert.IsFalse(_manager.HasSubscription("   "));
    }

    [TestMethod]
    public void GetActiveSubscriptions_ReturnsAllActiveSubscriptions()
    {
        // Arrange
        var sub1 = "test_sub_1";
        var sub2 = "test_sub_2";
        var sub3 = "test_sub_3";
        
        _manager.AddSubscription(sub1);
        _manager.AddSubscription(sub2);
        _manager.AddSubscription(sub3);

        // Act
        var activeSubscriptions = _manager.GetActiveSubscriptions().ToList();

        // Assert
        Assert.AreEqual(3, activeSubscriptions.Count);
        Assert.IsTrue(activeSubscriptions.Contains(sub1));
        Assert.IsTrue(activeSubscriptions.Contains(sub2));
        Assert.IsTrue(activeSubscriptions.Contains(sub3));
    }

    [TestMethod]
    public void GetActiveSubscriptions_ReturnsEmptyWhenNoSubscriptions()
    {
        // Act
        var activeSubscriptions = _manager.GetActiveSubscriptions().ToList();

        // Assert
        Assert.AreEqual(0, activeSubscriptions.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddSubscription_ThrowsForNullId()
    {
        // Act & Assert
        _manager.AddSubscription(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddSubscription_ThrowsForEmptyId()
    {
        // Act & Assert
        _manager.AddSubscription("");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddSubscription_ThrowsForWhitespaceId()
    {
        // Act & Assert
        _manager.AddSubscription("   ");
    }

    [TestMethod]
    public void RemoveSubscription_HandlesNullOrEmptyIdGracefully()
    {
        // Act & Assert (should not throw)
        _manager.RemoveSubscription(null!);
        _manager.RemoveSubscription("");
        _manager.RemoveSubscription("   ");
    }

    [TestMethod]
    public void AddSubscription_HandlesDuplicateIdsGracefully()
    {
        // Arrange
        var subscriptionId = "test_sub_1";

        // Act
        _manager.AddSubscription(subscriptionId);
        _manager.AddSubscription(subscriptionId); // Should not throw

        // Assert
        Assert.IsTrue(_manager.HasSubscription(subscriptionId));
        Assert.AreEqual(1, _manager.GetActiveSubscriptions().Count());
    }
}