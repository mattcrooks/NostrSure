namespace NostrSure.Domain.Entities;

public sealed class Pubkey
{
    public string Value { get; }

    public Pubkey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Pubkey cannot be empty.", nameof(value));
        Value = value;
    }

    /// <summary>
    /// Validates that the pubkey is a valid 32-byte hex string (64 characters).
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Value) &&
                          Value.Length == 64 &&
                          IsValidHex(Value);

    private static bool IsValidHex(string hex)
    {
        foreach (char c in hex)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}