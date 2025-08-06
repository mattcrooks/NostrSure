namespace NostrSure.Domain.Entities;

using NostrSure.Domain.Interfaces;
using NostrSure.Domain.ValueObjects;

public record NostrEvent(
    string Id,
    Pubkey Pubkey,
    DateTimeOffset CreatedAt,
    EventKind Kind,
    IReadOnlyList<NostrTag> Tags,
    string Content,
    string Sig
)
{
    public virtual bool Validate(INostrEventValidator validator, out List<string> errors)
    {
        errors = new List<string>();
        if (!validator.ValidateEventId(this, out var idError))
            errors.Add($"Event ID invalid: {idError}");
        if (!validator.ValidateSignature(this, out var sigError))
            errors.Add($"Signature invalid: {sigError}");
        if (!validator.ValidateKind(this, out var kindError))
            errors.Add($"Kind invalid: {kindError}");
        if (!validator.ValidateTags(this, out var tagError))
            errors.Add($"Tags invalid: {tagError}");
        return errors.Count == 0;
    }
}