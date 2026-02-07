using pq_chat_httpserver.Models;
using pq_chat_httpserver.Database;
using static BCrypt.Net.BCrypt;

namespace pq_chat_httpserver.Services;

public class UserService
{
    private readonly UserDatabase _userDatabase;
    public UserService(UserDatabase userDatabase)
    {
        _userDatabase = userDatabase;
    }

    public string GetAllUsers()
    {
        return "All Users in a string";
    }

    public async Task<User> CreateUser(string firstName, string lastName, string emailAddress, string password)
    {
        string userId         = Guid.NewGuid().ToString();
        string hashedPassword = HashPassword(password);

        var newUser = new User
        (
            userId, 
            firstName, 
            lastName, 
            emailAddress, 
            hashedPassword
        );

        User user = await _userDatabase.CreateUserAsync(newUser);
        return user;
    }

    public async Task<User?> LoginUser(string emailAddress, string password)
    {
        string inputHashedPassword = HashPassword(password);

        var user = await _userDatabase.GetUserCredentialsAsync(emailAddress);

        if (user is null)
            return null;

        if (Verify(password, user.HashedPassword))
            return user;
        else 
            return null;
    }
}