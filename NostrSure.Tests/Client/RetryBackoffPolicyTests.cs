using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;

namespace NostrSure.Tests.Client;

[TestCategory("Client")]
[TestClass]
public class RetryBackoffPolicyTests
{
    private RetryBackoffPolicy _policy = null!;

    [TestInitialize]
    public void Setup()
    {
        _policy = new RetryBackoffPolicy(
            baseDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromSeconds(10),
            maxRetries: 5);
    }

    [TestMethod]
    public void ShouldRetry_ReturnsTrueWhenAttemptIsWithinLimit()
    {
        // Act & Assert
        Assert.IsTrue(_policy.ShouldRetry(1));
        Assert.IsTrue(_policy.ShouldRetry(3));
        Assert.IsTrue(_policy.ShouldRetry(5));
    }

    [TestMethod]
    public void ShouldRetry_ReturnsFalseWhenAttemptExceedsLimit()
    {
        // Act & Assert
        Assert.IsFalse(_policy.ShouldRetry(6));
        Assert.IsFalse(_policy.ShouldRetry(10));
        Assert.IsFalse(_policy.ShouldRetry(100));
    }

    [TestMethod]
    public void GetDelay_ReturnsZeroForZeroOrNegativeAttempt()
    {
        // Act & Assert
        Assert.AreEqual(TimeSpan.Zero, _policy.GetDelay(0));
        Assert.AreEqual(TimeSpan.Zero, _policy.GetDelay(-1));
        Assert.AreEqual(TimeSpan.Zero, _policy.GetDelay(-10));
    }

    [TestMethod]
    public void GetDelay_ImplementsExponentialBackoff()
    {
        // Act
        var delay1 = _policy.GetDelay(1);
        var delay2 = _policy.GetDelay(2);
        var delay3 = _policy.GetDelay(3);

        // Assert
        // Each delay should be roughly double the previous (within jitter tolerance)
        Assert.IsTrue(delay1 > TimeSpan.Zero);
        Assert.IsTrue(delay2 > delay1);
        Assert.IsTrue(delay3 > delay2);
        
        // Verify exponential growth (allowing for jitter)
        Assert.IsTrue(delay2 >= delay1 * 1.5); // At least 1.5x due to jitter
        Assert.IsTrue(delay3 >= delay2 * 1.5);
    }

    [TestMethod]
    public void GetDelay_RespectsMaxDelay()
    {
        // Arrange
        var policyWithLowMax = new RetryBackoffPolicy(
            baseDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(2),
            maxRetries: 10);

        // Act
        var delay10 = policyWithLowMax.GetDelay(10);

        // Assert
        // Even with high attempts, delay should not exceed max (allowing for jitter)
        Assert.IsTrue(delay10 <= TimeSpan.FromSeconds(2.5)); // Max + 25% jitter
    }

    [TestMethod]
    public async Task DelayAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _policy.DelayAsync(1);
        stopwatch.Stop();

        // Assert
        // Should complete in reasonable time (base delay is 100ms, with jitter could be up to ~125ms)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds >= 50); // At least some delay
        Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 500); // But not too long
    }

    [TestMethod]
    public async Task DelayAsync_CanBeCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(
            async () => await _policy.DelayAsync(5, cts.Token));
    }

    [TestMethod]
    public async Task DelayAsync_ZeroAttemptCompletsesImmediately()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _policy.DelayAsync(0);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 50); // Should be nearly immediate
    }

    [TestMethod]
    public void GetDelay_HandlesVeryHighAttempts()
    {
        // Act
        var delay = _policy.GetDelay(100); // Very high attempt

        // Assert
        // Should not overflow or throw, and should respect max delay
        Assert.IsTrue(delay >= TimeSpan.Zero);
        Assert.IsTrue(delay <= TimeSpan.FromSeconds(12.5)); // Max + jitter tolerance
    }

    [TestMethod]
    public void Constructor_WithDefaultValues_CreatesValidPolicy()
    {
        // Act
        var defaultPolicy = new RetryBackoffPolicy();

        // Assert
        Assert.IsTrue(defaultPolicy.ShouldRetry(1));
        Assert.IsTrue(defaultPolicy.ShouldRetry(5));
        Assert.IsFalse(defaultPolicy.ShouldRetry(6));
        
        var delay = defaultPolicy.GetDelay(1);
        Assert.IsTrue(delay > TimeSpan.Zero);
    }
}