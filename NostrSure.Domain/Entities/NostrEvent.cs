using System;
using System.Collections.Generic;

namespace NostrSure.Domain.Entities;

using NostrSure.Domain.ValueObjects;

public interface INostrEventValidator
{
    bool ValidateSignature(NostrEvent evt, out string error);
    bool ValidateKind(NostrEvent evt, out string error);
    bool ValidateTags(NostrEvent evt, out string error);
    bool ValidateEventId(NostrEvent evt, out string error);
}

public sealed record NostrEvent(
    string Id,
    Pubkey Pubkey,
    DateTimeOffset CreatedAt,
    EventKind Kind,
    IReadOnlyList<IReadOnlyList<string>> Tags,
    string Content,
    string Sig
)
{
    public bool Validate(INostrEventValidator validator, out List<string> errors)
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