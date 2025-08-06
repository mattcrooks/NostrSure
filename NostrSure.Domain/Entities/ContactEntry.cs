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
        var values = BuildPTagValues();
        return new NostrTag("p", values);
    }

    /// <summary>
    /// Builds the values list for the "p" tag, handling optional fields explicitly.
    /// </summary>
    private List<string> BuildPTagValues()
    {
        var values = new List<string> { ContactPubkey.Value };

        bool hasPetname = !string.IsNullOrWhiteSpace(Petname);
        bool hasRelayUrl = !string.IsNullOrWhiteSpace(RelayUrl);

        if (hasPetname)
        {
            values.Add(Petname!);
        }
        else if (hasRelayUrl)
        {
            // Petname slot must be present (as empty string) if relay URL is present
            values.Add(string.Empty);
        }

        if (hasRelayUrl)
        {
            values.Add(RelayUrl!);
        }

        return values;
    }
}
