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

    [TestMethod]
    public void AddSubscription_StoresTimestampCorrectly()
    {
        var subscriptionId = "test_sub_time";
        _manager.AddSubscription(subscriptionId);
        var before = DateTime.UtcNow.AddSeconds(-1);
        var after = DateTime.UtcNow.AddSeconds(1);
        var dictField = typeof(InMemorySubscriptionManager)
            .GetField("_subscriptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>)dictField!.GetValue(_manager)!;
        Assert.IsTrue(dict.TryGetValue(subscriptionId, out var timestamp));
        Assert.IsTrue(timestamp >= before && timestamp <= after);
    }

    [TestMethod]
    public void RemoveSubscription_RemovesFromEnumeration()
    {
        var sub1 = "sub_enum_1";
        var sub2 = "sub_enum_2";
        _manager.AddSubscription(sub1);
        _manager.AddSubscription(sub2);
        _manager.RemoveSubscription(sub1);
        var active = _manager.GetActiveSubscriptions().ToList();
        Assert.AreEqual(1, active.Count);
        Assert.AreEqual(sub2, active[0]);
    }

    [TestMethod]
    public void NewSubscriptionId_FormatIsStrict()
    {
        var id = _manager.NewSubscriptionId();
        var parts = id.Split('_');
        Assert.AreEqual(3, parts.Length);
        Assert.AreEqual("sub", parts[0]);
        Assert.IsTrue(int.TryParse(parts[1], out _));
        Assert.IsTrue(DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out _));
    }

    [TestMethod]
    public void AddSubscription_ConcurrentAccess_IsThreadSafe()
    {
        var ids = Enumerable.Range(0, 1000).Select(i => $"sub_conc_{i}").ToList();
        System.Threading.Tasks.Parallel.ForEach(ids, id => _manager.AddSubscription(id));
        var active = _manager.GetActiveSubscriptions().ToList();
        Assert.AreEqual(1000, active.Count);
        foreach (var id in ids)
            Assert.IsTrue(active.Contains(id));
    }

    [TestMethod]
    public void RemoveSubscription_ConcurrentAccess_IsThreadSafe()
    {
        var ids = Enumerable.Range(0, 1000).Select(i => $"sub_conc_{i}").ToList();
        foreach (var id in ids)
            _manager.AddSubscription(id);
        System.Threading.Tasks.Parallel.ForEach(ids, id => _manager.RemoveSubscription(id));
        var active = _manager.GetActiveSubscriptions().ToList();
        Assert.AreEqual(0, active.Count);
    }
}