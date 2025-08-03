namespace NostrSure.Domain.Entities;

public sealed class NostrTag
{
    public string Name { get; }
    public IReadOnlyList<string> Values { get; }

    public NostrTag(string name, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name must not be empty", nameof(name));
        Name = name.ToLowerInvariant();
        Values = values?.ToList() ?? throw new ArgumentNullException(nameof(values));
    }

    public static NostrTag FromArray(IReadOnlyList<string> arr)
    {
        if (arr == null || arr.Count == 0)
            throw new ArgumentException("Tag array must have at least one element");
        return new NostrTag(arr[0], arr.Skip(1));
    }

    // Optional: Validate known tag types
    public bool IsValid()
    {
        // Example: "p" and "e" tags must have a second element that is a 64-char hex string
        if ((Name == "p" || Name == "e") && (Values.Count < 1 || Values[0].Length != 64))
            return false;
        // Add more rules as needed
        return true;
    }
}

