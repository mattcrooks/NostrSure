namespace NostrSure.Domain.Entities;

public sealed class Follow
{
    public Pubkey Follower { get; }
    public Pubkey Followee { get; }
    public Follow(Pubkey follower, Pubkey followee)
    {
        Follower = follower ?? throw new ArgumentNullException(nameof(follower));
        Followee = followee ?? throw new ArgumentNullException(nameof(followee));
    }
}