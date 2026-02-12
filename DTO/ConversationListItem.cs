namespace pq_chat_httpserver.DTO;
public class ConversationListItem
{
    public string    ConversationId     { get; set; } = string.Empty;
    public string    ConversationType   { get; set; } = string.Empty;
    public string?   OtherUserId        { get; set; }
    public string?   OtherUsername      { get; set; }
    public string?   OtherFirstName     { get; set; }
    public string?   OtherLastName      { get; set; }
    public string?   Title              { get; set; }
    public string?   AvatarUrl          { get; set; }
    public string?   LastMessagePreview { get; set; }
    public DateTime? LastMessageAt      { get; set; }
}
