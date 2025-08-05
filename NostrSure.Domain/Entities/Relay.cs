namespace NostrSure.Domain.Entities;

public sealed class Relay
{
    public Uri Endpoint { get; }
    public Relay(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid relay endpoint.", nameof(endpoint));
        
        // Validate that the scheme is appropriate for a relay
        if (uri.Scheme != "ws" && uri.Scheme != "wss" && uri.Scheme != "http" && uri.Scheme != "https")
            throw new ArgumentException("Relay endpoint must use ws://, wss://, http://, or https:// scheme.", nameof(endpoint));
        
        Endpoint = uri;
    }
}