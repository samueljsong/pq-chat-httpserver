namespace pq_chat_httpserver.Models;

public class UserSearchResult
{
    public string UserId    { get; set; }
    public string Username  { get; set; }
    public string FirstName { get; set; }
    public string LastName  { get; set; }

    public UserSearchResult(string userId, string username, string firstName, string lastName)
    {
        UserId    = userId;
        Username  = username;
        FirstName = firstName;
        LastName  = lastName;
    }
}
