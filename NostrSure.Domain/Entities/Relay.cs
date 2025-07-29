namespace NostrSure.Domain.Entities;

public sealed class Relay
{
    public Uri Endpoint { get; }
    public Relay(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid relay endpoint.", nameof(endpoint));
        Endpoint = uri;
    }
}