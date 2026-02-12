using pq_chat_httpserver.Models;
using MySql.Data.MySqlClient;
using pq_chat_httpserver.DTO;

namespace pq_chat_httpserver.Database;

public class FriendshipDatabase
{
    private readonly string _connectionString = "Server=localhost;Port=3306;Database=sys;User=root;Password=;";

    public async Task CreateFriendRequestAsync(string userLowId, string userHighId, string requesterUserId, string recipientUserId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1) Check if friendship pair already exists
            const string selectQuery = @"
                SELECT friendship_id, status, requested_by_user_id
                FROM sys.friendships
                WHERE user_low_id = @low AND user_high_id = @high
                LIMIT 1;";

            string? existingId          = null;
            string? existingStatus      = null;
            string? existingRequestedBy = null;

            await using (var selectCommand = new MySqlCommand(selectQuery, connection, (MySqlTransaction)transaction))
            {
                selectCommand.Parameters.AddWithValue( "@low" , userLowId  );
                selectCommand.Parameters.AddWithValue( "@high", userHighId );

                await using var reader = await selectCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    existingId          = reader.GetString(reader.GetOrdinal("friendship_id"));
                    existingStatus      = reader.GetString(reader.GetOrdinal("status"));
                    existingRequestedBy = reader.GetString(reader.GetOrdinal("requested_by_user_id"));
                }
            }

            // Helper for notification insert
            async Task InsertFriendRequestNotificationAsync(string friendshipId)
            {
                const string notificationQuery = @"
                    INSERT INTO sys.notifications
                    (
                        notification_id,
                        recipient_user_id,
                        actor_user_id,
                        notification_type,
                        friendship_id,
                        is_read,
                        created_at,
                        title,
                        body
                    )
                    VALUES
                    (
                        @notifId,
                        @recipient,
                        @actor,
                        'FRIEND_REQUEST',
                        @friendshipId,
                        0,
                        NOW(),
                        'Friend request',
                        'You have a new friend request'
                    );";

                await using var notificationCommand = new MySqlCommand(notificationQuery, connection, (MySqlTransaction)transaction);
                notificationCommand.Parameters.AddWithValue( "@notifId"      , Guid.NewGuid().ToString() );
                notificationCommand.Parameters.AddWithValue( "@recipient"    , recipientUserId           );
                notificationCommand.Parameters.AddWithValue( "@actor"        , requesterUserId           );
                notificationCommand.Parameters.AddWithValue( "@friendshipId" , friendshipId              );

                await notificationCommand.ExecuteNonQueryAsync();
            }

            // 2) Decide insert/update based on existing row
            if (existingId is null)
            {
                // Insert new friendship row
                var friendshipId = Guid.NewGuid().ToString();

                const string insertQuery = @"
                    INSERT INTO sys.friendships
                    (
                        friendship_id,
                        user_low_id,
                        user_high_id,
                        requested_by_user_id,
                        status,
                        created_at,
                        responded_at
                    )
                    VALUES
                    (
                        @id,
                        @low,
                        @high,
                        @requestedBy,
                        'PENDING',
                        NOW(),
                        NULL
                    );";

                await using var insertCommand = new MySqlCommand(insertQuery, connection, (MySqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue( "@id"          , friendshipId    );
                insertCommand.Parameters.AddWithValue( "@low"         , userLowId       );
                insertCommand.Parameters.AddWithValue( "@high"        , userHighId      );
                insertCommand.Parameters.AddWithValue( "@requestedBy" , requesterUserId );

                await insertCommand.ExecuteNonQueryAsync();

                await InsertFriendRequestNotificationAsync(friendshipId);
            }
            else
            {
                // Already exists
                switch (existingStatus)
                {
                    case "ACCEPTED":
                        throw new InvalidOperationException("You are already friends.");

                    case "BLOCKED":
                        throw new InvalidOperationException("Cannot send friend request.");

                    case "PENDING":
                        // If pending already, you can just return (or throw)
                        // Optional: if the other user already requested you, you could auto-accept here
                        throw new InvalidOperationException("Friend request already pending.");

                    case "DECLINED":
                        // Re-request: flip back to pending and set requested_by
                        const string updateQuery = @"
                            UPDATE sys.friendships
                            SET status = 'PENDING',
                                requested_by_user_id = @requestedBy,
                                created_at = NOW(),
                                responded_at = NULL
                            WHERE friendship_id = @id;";

                        await using (var updateCmd = new MySqlCommand(updateQuery, connection, (MySqlTransaction)transaction))
                        {
                            updateCmd.Parameters.AddWithValue( "@requestedBy" , requesterUserId );
                            updateCmd.Parameters.AddWithValue( "@id"          , existingId      );

                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        await InsertFriendRequestNotificationAsync(existingId);
                        break;

                    default:
                        throw new InvalidOperationException("Unknown friendship status.");
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<FriendshipList>> GetIncomingRequestsAsync(string userId)
    {
        // Incoming = someone else requested you (status PENDING)
        const string sql = @"
            SELECT
                f.friendship_id,
                f.status,
                f.created_at,
                u.user_id     AS other_user_id,
                u.username    AS other_username,
                u.first_name  AS other_first_name,
                u.last_name   AS other_last_name
            FROM sys.friendships f
            JOIN sys.users u
            ON u.user_id = f.requested_by_user_id
            WHERE f.status = 'PENDING'
            AND f.requested_by_user_id <> @me
            AND (@me = f.user_low_id OR @me = f.user_high_id)
            ORDER BY f.created_at DESC;";

        return await ExecuteFriendshipListQueryAsync(sql, userId, direction: "INCOMING");
    }

    public async Task<List<FriendshipList>> GetPendingRequestsAsync(string userId)
    {
        // Outgoing = you requested someone else (status PENDING)
        const string sql = @"
            SELECT
                f.friendship_id,
                f.status,
                f.created_at,
                u.user_id     AS other_user_id,
                u.username    AS other_username,
                u.first_name  AS other_first_name,
                u.last_name   AS other_last_name
            FROM sys.friendships f
            JOIN sys.users u
                ON u.user_id =
                    CASE
                        WHEN f.user_low_id = @me THEN f.user_high_id
                        ELSE f.user_low_id
                    END
            WHERE f.status = 'PENDING'
                AND f.requested_by_user_id = @me
                AND (@me = f.user_low_id OR @me = f.user_high_id)
            ORDER BY f.created_at DESC;";

        return await ExecuteFriendshipListQueryAsync(sql, userId, direction: "OUTGOING");
    }

    public async Task<List<FriendshipList>> GetFriendsAsync(string userId)
    {
        // Friends = ACCEPTED
        const string sql = @"
            SELECT
                f.friendship_id,
                f.status,
                f.created_at,
                u.user_id     AS other_user_id,
                u.username    AS other_username,
                u.first_name  AS other_first_name,
                u.last_name   AS other_last_name
            FROM sys.friendships f
            JOIN sys.users u
                ON u.user_id =
                    CASE
                        WHEN f.user_low_id = @me THEN f.user_high_id
                        ELSE f.user_low_id
                    END
            WHERE f.status = 'ACCEPTED'
                AND (@me = f.user_low_id OR @me = f.user_high_id)
            ORDER BY u.username ASC;";

        return await ExecuteFriendshipListQueryAsync(sql, userId, direction: "FRIEND");
    }

    // Shared mapper helper
    private async Task<List<FriendshipList>> ExecuteFriendshipListQueryAsync(string query, string userId, string direction)
    {
        var results = new List<FriendshipList>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@me", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var statusStr = reader.GetString(reader.GetOrdinal("status"));
            if (!Enum.TryParse(statusStr, ignoreCase: true, out FriendshipStatus status))
                status = FriendshipStatus.PENDING;

            results.Add(new FriendshipList
            {
                FriendshipId = reader.GetString(reader.GetOrdinal("friendship_id")),
                Direction = direction,
                Status = status,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),

                OtherUserId = reader.GetString(reader.GetOrdinal("other_user_id")),
                OtherUsername = reader.GetString(reader.GetOrdinal("other_username")),
                OtherFirstName = reader.GetString(reader.GetOrdinal("other_first_name")),
                OtherLastName = reader.GetString(reader.GetOrdinal("other_last_name")),
            });
        }

        return results;
    }

    public async Task AcceptFriendRequestAsync(string friendshipId, string userId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"
            UPDATE sys.friendships
            SET status = 'ACCEPTED',
                responded_at = NOW()
            WHERE friendship_id = @id
            AND status = 'PENDING';";

        await using var command = new MySqlCommand(query, connection);

        command.Parameters.AddWithValue("@id", friendshipId);

        await command.ExecuteNonQueryAsync();
    }


}