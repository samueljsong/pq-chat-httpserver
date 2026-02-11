using pq_chat_httpserver.Models;
using pq_chat_httpserver.Database;
using pq_chat_httpserver.Services;
using pq_chat_httpserver.DTO;
using static BCrypt.Net.BCrypt;
using System.Security.Cryptography.X509Certificates;

namespace pq_chat_httpserver.Services;

public class FriendshipService
{
    private readonly FriendshipDatabase _friendshipDatabase;
    
    public FriendshipService(FriendshipDatabase friendshipDatabase)
    {
        _friendshipDatabase = friendshipDatabase;
    }

    public async Task CreateFriendRequest(string recipientUserId, string requesterUserId)
    {
        var (lowId, highId) = OrderPair(requesterUserId, recipientUserId);

        await _friendshipDatabase.CreateFriendRequestAsync(lowId, highId, requesterUserId, recipientUserId);
    }

    public async Task<List<FriendshipList>> GetIncomingFriendRequests(string userId)
    {
        return await _friendshipDatabase.GetIncomingRequestsAsync(userId);  
    } 

    public async Task<List<FriendshipList>> GetAllFriends(string userId)
    {
        return await _friendshipDatabase.GetFriendsAsync(userId);
    }

    public async Task<List<FriendshipList>> GetPendingFriendRequests(string userId)
    {
        return await _friendshipDatabase.GetPendingRequestsAsync(userId);
    }

    private static (string low, string high) OrderPair(string a, string b)
    {
        return string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
    }
}