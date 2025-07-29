namespace NostrSure.Domain.Entities;

public sealed class Pubkey
{
    public string Value { get; }
    public Pubkey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Pubkey cannot be empty.", nameof(value));
        Value = value;
    }
}