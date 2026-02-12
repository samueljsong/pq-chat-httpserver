using pq_chat_httpserver.Database;
using pq_chat_httpserver.DTO;

namespace pq_chat_httpserver.Services;

public class ConversationService
{
    private readonly ConversationDatabase _conversationDatabase;

    public ConversationService(ConversationDatabase conversationDatabase)
    {
        _conversationDatabase = conversationDatabase;
    }

    public Task<string> CreateOrGetDmAsync(string requesterUserId, string recipientUserId)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
            throw new ArgumentException("Missing other user id");

        if (requesterUserId == recipientUserId)
            throw new ArgumentException("Cannot create DM with yourself");

        return _conversationDatabase.CreateOrGetDmAsync(requesterUserId, recipientUserId);
    }

    public Task<List<ConversationListItem>> GetAllForUserAsync(string userId)
        => _conversationDatabase.GetAllForUserAsync(userId);
}