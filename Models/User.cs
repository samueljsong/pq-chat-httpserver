namespace pq_chat_httpserver.Models;

public class User
{
    public string UserId         {get;}
    public string FirstName      {get;}
    public string LastName       {get;}
    public string EmailAddress   {get;}
    public string HashedPassword {get;}

    public User(string userId, string firstName, string lastName, string emailAddress, string hashedPassword)
    {
        UserId         = userId;
        FirstName      = firstName;
        LastName       = lastName;
        EmailAddress   = emailAddress;
        HashedPassword = hashedPassword;
    }
}