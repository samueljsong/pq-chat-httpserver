using pq_chat_httpserver.Models;
using MySql.Data.MySqlClient;

namespace pq_chat_httpserver.Database;

public class UserDatabase
{
    private readonly string _connectionString = "Server=localhost;Port=3306;Database=sys;User=root;Password=;";

    public async Task<User> CreateUserAsync(User user)
    {
        string query = @"
            INSERT INTO users (user_id, first_name, last_name, email_address, hashed_password)
            VALUES (@UserId, @Firstname, @LastName, @EmailAddress, @HashedPassword);
        ";

        using (MySqlConnection connection = new MySqlConnection(_connectionString))
        {
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("Connection is successful");

                using(var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue( "@UserId"        , user.UserId         );
                    command.Parameters.AddWithValue( "@FirstName"     , user.FirstName      );
                    command.Parameters.AddWithValue( "@LastName"      , user.LastName       );
                    command.Parameters.AddWithValue( "@EmailAddress"  , user.EmailAddress   );
                    command.Parameters.AddWithValue( "@HashedPassword", user.HashedPassword );

                    int rowCount = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"number of rows inserted: {rowCount}");
                }

                return user;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return user;
            }
        }
    }
}