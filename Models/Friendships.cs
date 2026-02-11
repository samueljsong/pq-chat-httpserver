namespace pq_chat_httpserver.Models;

public class Friendship
{
    public string           FriendshipId      { get; set; } = string.Empty;
    public string           UserLowId         { get; set; } = string.Empty;
    public string           UserHighId        { get; set; } = string.Empty;
    public string           RequestedByUserId { get; set; } = string.Empty;
    public FriendshipStatus Status            { get; set; }
    public DateTime         CreatedAt         { get; set; }
    public DateTime?        RespondedAt       { get; set; }

    public Friendship(string friendshipId, string userLowId, string userHighId, string requestedByUserId, FriendshipStatus status)
    {
        FriendshipId      = friendshipId;
        UserLowId         = userLowId;
        UserHighId        = userHighId;
        RequestedByUserId = requestedByUserId;
        Status            = status;
        CreatedAt         = DateTime.UtcNow;
    }
}

