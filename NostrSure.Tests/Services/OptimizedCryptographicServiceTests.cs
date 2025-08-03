using NostrSure.Domain.Services;

namespace NostrSure.Tests.Services;

[TestClass]
public class OptimizedCryptographicServiceTests
{
    private OptimizedCryptographicService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new OptimizedCryptographicService();
    }

    [TestMethod]
    public void VerifySchnorrSignature_InvalidSignatureLength_ReturnsFalse()
    {
        // Arrange
        var invalidSignature = new byte[63]; // Should be 64 bytes
        var validMessage = new byte[32];
        var validPublicKey = new byte[32];

        // Act
        var result = _service.VerifySchnorrSignature(invalidSignature, validMessage, validPublicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_InvalidMessageLength_ReturnsFalse()
    {
        // Arrange
        var validSignature = new byte[64];
        var invalidMessage = new byte[31]; // Should be 32 bytes
        var validPublicKey = new byte[32];

        // Act
        var result = _service.VerifySchnorrSignature(validSignature, invalidMessage, validPublicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_InvalidPublicKeyLength_ReturnsFalse()
    {
        // Arrange
        var validSignature = new byte[64];
        var validMessage = new byte[32];
        var invalidPublicKey = new byte[31]; // Should be 32 bytes

        // Act
        var result = _service.VerifySchnorrSignature(validSignature, validMessage, invalidPublicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_InvalidPublicKey_ReturnsFalse()
    {
        // Arrange
        var validSignature = new byte[64];
        var validMessage = new byte[32];
        var invalidPublicKey = new byte[32]; // All zeros - invalid public key

        // Act
        var result = _service.VerifySchnorrSignature(validSignature, validMessage, invalidPublicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_InvalidSignatureFormat_ReturnsFalse()
    {
        // Arrange - Use a potentially valid public key but invalid signature
        var invalidSignature = new byte[64]; // All zeros - invalid signature format
        var validMessage = new byte[32];
        var publicKey = new byte[32];
        // Set a potentially valid x-coordinate for public key
        publicKey[0] = 0x02;

        // Act
        var result = _service.VerifySchnorrSignature(invalidSignature, validMessage, publicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_EmptyInputs_ReturnsFalse()
    {
        // Arrange
        var emptySignature = ReadOnlySpan<byte>.Empty;
        var emptyMessage = ReadOnlySpan<byte>.Empty;
        var emptyPublicKey = ReadOnlySpan<byte>.Empty;

        // Act
        var result = _service.VerifySchnorrSignature(emptySignature, emptyMessage, emptyPublicKey);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifySchnorrSignature_TooLongInputs_ReturnsFalse()
    {
        // Arrange
        var tooLongSignature = new byte[65]; // Too long
        var tooLongMessage = new byte[33]; // Too long
        var tooLongPublicKey = new byte[33]; // Too long

        // Act
        var result = _service.VerifySchnorrSignature(tooLongSignature, tooLongMessage, tooLongPublicKey);

        // Assert
        Assert.IsFalse(result);
    }
}