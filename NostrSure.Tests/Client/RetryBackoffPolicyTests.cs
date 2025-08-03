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
        // Act - Sample multiple times to account for jitter randomness
        var delay1Samples = Enumerable.Range(0, 20).Select(_ => _policy.GetDelay(1)).ToList();
        var delay2Samples = Enumerable.Range(0, 20).Select(_ => _policy.GetDelay(2)).ToList();
        var delay3Samples = Enumerable.Range(0, 20).Select(_ => _policy.GetDelay(3)).ToList();

        // Assert
        // All delays should be positive
        Assert.IsTrue(delay1Samples.All(d => d > TimeSpan.Zero));
        Assert.IsTrue(delay2Samples.All(d => d > TimeSpan.Zero));
        Assert.IsTrue(delay3Samples.All(d => d > TimeSpan.Zero));

        // Average delays should follow exponential pattern (jitter cancels out over samples)
        var avgDelay1 = delay1Samples.Select(d => d.TotalMilliseconds).Average();
        var avgDelay2 = delay2Samples.Select(d => d.TotalMilliseconds).Average();
        var avgDelay3 = delay3Samples.Select(d => d.TotalMilliseconds).Average();

        // Verify exponential growth in averages (should be approximately 2x)
        Assert.IsTrue(avgDelay2 >= avgDelay1 * 1.7, $"Expected avgDelay2 ({avgDelay2}) >= avgDelay1 * 1.7 ({avgDelay1 * 1.7})");
        Assert.IsTrue(avgDelay3 >= avgDelay2 * 1.7, $"Expected avgDelay3 ({avgDelay3}) >= avgDelay2 * 1.7 ({avgDelay2 * 1.7})");
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