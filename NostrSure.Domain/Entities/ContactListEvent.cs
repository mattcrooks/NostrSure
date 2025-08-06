namespace NostrSure.Domain.Entities;

using NostrSure.Domain.ValueObjects;

/// <summary>
/// Represents a NIP-02 Contact List Event (kind 3).
/// Immutable record following SOLID principles with proper encapsulation.
/// </summary>
public sealed record ContactListEvent(
    string Id,
    Pubkey Pubkey,
    DateTimeOffset CreatedAt,
    IReadOnlyList<NostrTag> Tags,
    string Content,
    string Sig,
    IReadOnlyList<ContactEntry> Contacts
) : NostrEvent(Id, Pubkey, CreatedAt, EventKind.ContactList, Tags, Content, Sig)
{
    /// <summary>
    /// Creates a ContactListEvent from a base NostrEvent and extracts contacts from p tags.
    /// </summary>
    public static ContactListEvent FromNostrEvent(NostrEvent nostrEvent)
    {
        if (nostrEvent.Kind != EventKind.ContactList)
            throw new ArgumentException("NostrEvent must be of kind ContactList", nameof(nostrEvent));

        var contacts = ExtractContactsFromTags(nostrEvent.Tags);

        return new ContactListEvent(
            nostrEvent.Id,
            nostrEvent.Pubkey,
            nostrEvent.CreatedAt,
            nostrEvent.Tags,
            nostrEvent.Content,
            nostrEvent.Sig,
            contacts
        );
    }

    /// <summary>
    /// Creates a new ContactListEvent with the given contacts, automatically generating p tags.
    /// </summary>
    public static ContactListEvent Create(
        string id,
        Pubkey pubkey,
        DateTimeOffset createdAt,
        string content,
        string sig,
        IReadOnlyList<ContactEntry> contacts,
        IReadOnlyList<NostrTag>? additionalTags = null)
    {
        var allTags = new List<NostrTag>();

        // Add non-p tags first
        if (additionalTags != null)
            allTags.AddRange(additionalTags.Where(t => t.Name != "p"));

        // Add p tags from contacts
        allTags.AddRange(contacts.Select(c => c.ToPTag()));

        return new ContactListEvent(
            id,
            pubkey,
            createdAt,
            allTags,
            content,
            sig,
            contacts
        );
    }

    /// <summary>
    /// Extracts contact entries from p tags in the tags collection.
    /// </summary>
    private static IReadOnlyList<ContactEntry> ExtractContactsFromTags(IReadOnlyList<NostrTag> tags)
    {
        return tags
            .Where(tag => tag.Name == "p" && tag.Values.Count > 0)
            .Select(ContactEntry.FromPTag)
            .ToList();
    }

    /// <summary>
    /// Validates the contact list event according to NIP-02 specifications.
    /// </summary>
    public bool IsValidContactList()
    {
        // All contacts must be valid
        if (!Contacts.All(c => c.IsValid))
            return false;

        // Check that p tags match contacts
        var pTags = Tags.Where(t => t.Name == "p").ToList();
        if (pTags.Count != Contacts.Count)
            return false;

        // Verify each contact has a corresponding p tag
        for (int i = 0; i < Contacts.Count; i++)
        {
            var expectedTag = Contacts[i].ToPTag();
            if (!pTags.Any(tag => TagsMatch(tag, expectedTag)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Override validation to include NIP-02 specific validation.
    /// </summary>
    public override bool Validate(Interfaces.INostrEventValidator validator, out List<string> errors)
    {
        // First run base validation
        var isValid = base.Validate(validator, out errors);

        // Then add NIP-02 specific validation
        if (!IsValidContactList())
        {
            errors.Add("Contact list validation failed: contacts and p tags do not match");
            isValid = false;
        }

        // Validate individual contacts
        for (int i = 0; i < Contacts.Count; i++)
        {
            if (!Contacts[i].IsValid)
            {
                errors.Add($"Contact {i} is invalid: {Contacts[i].ContactPubkey.Value}");
                isValid = false;
            }
        }

        return isValid;
    }

    private static bool TagsMatch(NostrTag tag1, NostrTag tag2)
    {
        if (tag1.Name != tag2.Name || tag1.Values.Count != tag2.Values.Count)
            return false;

        for (int i = 0; i < tag1.Values.Count; i++)
        {
            if (tag1.Values[i] != tag2.Values[i])
                return false;
        }

        return true;
    }
}
