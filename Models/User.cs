namespace pq_chat_httpserver.Models;

public class User
{
    private readonly string UserId;
    private readonly string Name;
    private readonly string Email;

    public User(string userId, string name, string email)
    {
        UserId = userId;
        Name   = name;
        Email  = email;
    }
}
