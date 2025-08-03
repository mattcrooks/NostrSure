using NostrSure.Domain.Validation;

namespace NostrSure.Tests.Validation;

[TestClass]
public class ValidationResultTests
{
    [TestMethod]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.IsNull(result.Error);
        Assert.AreEqual(ValidationSeverity.Warning, result.Severity); // Default severity is Warning, not Error
    }

    [TestMethod]
    public void Failure_WithMessage_CreatesInvalidResult()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var result = ValidationResult.Failure(message);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(message, result.Error.Message);
        Assert.IsNull(result.Error.Code);
        Assert.IsNull(result.Error.InnerException);
        Assert.AreEqual(ValidationSeverity.Error, result.Severity);
    }

    [TestMethod]
    public void Failure_WithMessageAndCode_CreatesInvalidResult()
    {
        // Arrange
        const string message = "Test error message";
        const string code = "TEST_ERROR";

        // Act
        var result = ValidationResult.Failure(message, code);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(message, result.Error.Message);
        Assert.AreEqual(code, result.Error.Code);
        Assert.IsNull(result.Error.InnerException);
        Assert.AreEqual(ValidationSeverity.Error, result.Severity);
    }

    [TestMethod]
    public void Failure_WithMessageAndSeverity_CreatesInvalidResult()
    {
        // Arrange
        const string message = "Test warning message";

        // Act
        var result = ValidationResult.Failure(message, ValidationSeverity.Warning);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(message, result.Error.Message);
        Assert.AreEqual(ValidationSeverity.Warning, result.Severity);
    }

    [TestMethod]
    public void Failure_WithException_CreatesInvalidResult()
    {
        // Arrange
        const string message = "Test error with exception";
        var exception = new InvalidOperationException("Inner exception");

        // Act
        var result = ValidationResult.Failure(message, exception);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(message, result.Error.Message);
        Assert.AreEqual(exception, result.Error.InnerException);
        Assert.AreEqual(ValidationSeverity.Error, result.Severity);
    }

    [TestMethod]
    public void Failure_WithExceptionAndCriticalSeverity_CreatesInvalidResult()
    {
        // Arrange
        const string message = "Critical error";
        var exception = new ArgumentException("Critical exception");

        // Act
        var result = ValidationResult.Failure(message, exception, ValidationSeverity.Critical);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(message, result.Error.Message);
        Assert.AreEqual(exception, result.Error.InnerException);
        Assert.AreEqual(ValidationSeverity.Critical, result.Severity);
    }

    [TestMethod]
    public void ValidationError_DefaultConstructor_SetsAllProperties()
    {
        // Arrange
        const string message = "Error message";
        const string code = "ERROR_CODE";
        var exception = new Exception("Inner exception");

        // Act
        var error = new ValidationError(message, code, exception);

        // Assert
        Assert.AreEqual(message, error.Message);
        Assert.AreEqual(code, error.Code);
        Assert.AreEqual(exception, error.InnerException);
    }

    [TestMethod]
    public void ValidationSeverity_EnumValues_AreCorrect()
    {
        // Assert
        Assert.AreEqual(0, (int)ValidationSeverity.Warning);
        Assert.AreEqual(1, (int)ValidationSeverity.Error);
        Assert.AreEqual(2, (int)ValidationSeverity.Critical);
    }
}