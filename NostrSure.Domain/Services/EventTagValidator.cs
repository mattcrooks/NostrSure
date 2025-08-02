using System.Linq;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// Validates tag structure and content according to NIP-01 rules
/// </summary>
public sealed class EventTagValidator : IEventTagValidator
{
    public ValidationResult ValidateTags(NostrEvent evt)
    {
        if (evt.Tags == null)
        {
            return ValidationResult.Failure("Tags are null", "NULL_TAGS");
        }
        
        foreach (var tag in evt.Tags)
        {
            if (tag == null)
            {
                return ValidationResult.Failure("Tag is null", "NULL_TAG");
            }
            
            if (string.IsNullOrWhiteSpace(tag.Name))
            {
                return ValidationResult.Failure("Tag name is empty or null", "EMPTY_TAG_NAME");
            }
            
            // Check if tag is valid according to NIP-01 rules
            if (!tag.IsValid())
            {
                return ValidationResult.Failure(
                    $"Invalid tag: {tag.Name}", 
                    "INVALID_TAG_FORMAT");
            }
            
            // Check for empty values in tag
            if (tag.Values.Any(string.IsNullOrWhiteSpace))
            {
                return ValidationResult.Failure(
                    "Tag contains empty value", 
                    "EMPTY_TAG_VALUE");
            }
        }
        
        return ValidationResult.Success();
    }
}