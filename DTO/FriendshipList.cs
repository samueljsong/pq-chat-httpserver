namespace pq_chat_httpserver.DTO;

public class FriendshipList
{
    public string           FriendshipId   { get; set; } = string.Empty;
    public string           Direction      { get; set; } = string.Empty;
    public FriendshipStatus Status         { get; set; }
    public DateTime         CreatedAt      { get; set; }
    public string           OtherUserId    { get; set; } = string.Empty;
    public string           OtherUsername  { get; set; } = string.Empty;
    public string           OtherFirstName { get; set; } = string.Empty;
    public string           OtherLastName  { get; set; } = string.Empty;
}