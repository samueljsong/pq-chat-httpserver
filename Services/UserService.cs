using pq_chat_httpserver.Models;
using pq_chat_httpserver.Database;
using pq_chat_httpserver.Services;
using static BCrypt.Net.BCrypt;
using System.Security.Cryptography.X509Certificates;

namespace pq_chat_httpserver.Services;

public class UserService
{
    private readonly UserDatabase _userDatabase;
    private readonly JwtService   _jwtService;
    public UserService(UserDatabase userDatabase, JwtService jwtService)
    {
        _userDatabase = userDatabase;
        _jwtService   = jwtService;
    }

    public string GetAllUsers()
    {
        return "All Users in a string";
    }

    public async Task<User> CreateUser(string username, string firstName, string lastName, string emailAddress, string password)
    {
        string userId         = Guid.NewGuid().ToString();
        string hashedPassword = HashPassword(password);

        var newUser = new User
        (
            userId, 
            username,
            firstName, 
            lastName, 
            emailAddress, 
            hashedPassword
        );

        User user = await _userDatabase.CreateUserAsync(newUser);
        return user;
    }

    public async Task<string?> LoginUser(string emailAddress, string password)
    {
        var user = await _userDatabase.GetUserCredentialsAsync(emailAddress);

        if (user is null)
            return null;

        if (!Verify(password, user.HashedPassword))
            return null;

        // issue JWT
        var token = _jwtService.GenerateToken(user.UserId, user.EmailAddress);
        return token;
    }

    public async Task<List<UserSearchResult>> SearchUsersByUsernamePrefixAsync(string prefix, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return new List<UserSearchResult>();

        prefix = prefix.Trim();

        return await _userDatabase.SearchUsersByUsernamePrefixAsync(prefix, limit);
    }
}