using NostrSure.Domain.Entities;

namespace NostrSure.Domain.Interfaces;

public interface INostrEventValidator
{
    bool ValidateSignature(NostrEvent evt, out string error);
    bool ValidateKind(NostrEvent evt, out string error);
    bool ValidateTags(NostrEvent evt, out string error);
    bool ValidateEventId(NostrEvent evt, out string error);
}
