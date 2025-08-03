namespace NostrSure.Domain.Validation;

/// <summary>
/// Represents the result of a validation operation with rich error information
/// </summary>
public readonly record struct ValidationResult
{
    public bool IsValid { get; init; }
    public ValidationError? Error { get; init; }
    public ValidationSeverity Severity { get; init; }

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(string message, ValidationSeverity severity = ValidationSeverity.Error)
        => new() { IsValid = false, Error = new ValidationError(message), Severity = severity };

    public static ValidationResult Failure(string message, string? code, ValidationSeverity severity = ValidationSeverity.Error)
        => new() { IsValid = false, Error = new ValidationError(message, code), Severity = severity };

    public static ValidationResult Failure(string message, Exception exception, ValidationSeverity severity = ValidationSeverity.Error)
        => new() { IsValid = false, Error = new ValidationError(message, InnerException: exception), Severity = severity };
}

/// <summary>
/// Represents detailed information about a validation error
/// </summary>
public record ValidationError(string Message, string? Code = null, Exception? InnerException = null);

/// <summary>
/// Indicates the severity level of a validation error
/// </summary>
public enum ValidationSeverity
{
    Warning,
    Error,
    Critical
}