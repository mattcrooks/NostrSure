using NostrSure.Domain.Entities;

namespace NostrSure.Tests.Entities;

[TestClass]
public class FollowTests
{
    [TestMethod]
    public void Constructor_ValidArguments_CreatesFollow()
    {
        // Arrange
        var follower = new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234");
        var followee = new Pubkey("efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678");

        // Act
        var follow = new Follow(follower, followee);

        // Assert
        Assert.AreEqual(follower, follow.Follower);
        Assert.AreEqual(followee, follow.Followee);
    }

    [TestMethod]
    public void Constructor_NullFollower_ThrowsArgumentNullException()
    {
        // Arrange
        var followee = new Pubkey("efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Follow(null!, followee));
    }

    [TestMethod]
    public void Constructor_NullFollowee_ThrowsArgumentNullException()
    {
        // Arrange
        var follower = new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new Follow(follower, null!));
    }
}