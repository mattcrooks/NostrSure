namespace NostrSure.Domain.Entities;

using NostrSure.Domain.ValueObjects;

/// <summary>
/// Represents a contact entry in a NIP-02 contact list.
/// Immutable value object following SOLID principles.
/// </summary>
public sealed record ContactEntry(
    Pubkey ContactPubkey,
    string? Petname = null,
    string? RelayUrl = null)
{
    /// <summary>
    /// Validates the contact entry according to NIP-02 specifications.
    /// </summary>
    public bool IsValid => ContactPubkey.IsValid &&
                          (string.IsNullOrEmpty(RelayUrl) || Uri.TryCreate(RelayUrl, UriKind.Absolute, out _));

    /// <summary>
    /// Creates a ContactEntry from a NostrTag "p" tag.
    /// </summary>
    public static ContactEntry FromPTag(NostrTag pTag)
    {
        if (pTag.Name != "p" || pTag.Values.Count == 0)
            throw new ArgumentException("Invalid p tag for contact entry", nameof(pTag));

        var pubkeyValue = pTag.Values[0];
        var petname = pTag.Values.ElementAtOrDefault(1);
        var relayUrl = pTag.Values.ElementAtOrDefault(2);

        return new ContactEntry(
            new Pubkey(pubkeyValue),
            string.IsNullOrWhiteSpace(petname) ? null : petname,
            string.IsNullOrWhiteSpace(relayUrl) ? null : relayUrl
        );
    }

    /// <summary>
    /// Converts this ContactEntry to a NostrTag "p" tag.
    /// </summary>
    public NostrTag ToPTag()
    {
        var values = new List<string> { ContactPubkey.Value };

        if (!string.IsNullOrWhiteSpace(Petname))
            values.Add(Petname);

        if (!string.IsNullOrWhiteSpace(RelayUrl))
        {
            // Ensure we have petname slot filled (can be empty string)
            if (values.Count == 1)
                values.Add(string.Empty);
            values.Add(RelayUrl);
        }

        return new NostrTag("p", values);
    }
}
