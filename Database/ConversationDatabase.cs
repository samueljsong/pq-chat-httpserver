using MySql.Data.MySqlClient;
using pq_chat_httpserver.DTO;

namespace pq_chat_httpserver.Database;

public class ConversationDatabase
{
    private readonly string _connectionString = "Server=localhost;Port=3306;Database=sys;User=root;Password=;";

    public async Task<string> CreateOrGetDmAsync(string meUserId, string otherUserId)
    {
        var (low, high) = OrderPair(meUserId, otherUserId);

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        var mysqlTx = (MySqlTransaction)tx;

        try
        {
            // 1) Verify friendship is ACCEPTED
            const string friendshipCheck = @"
                SELECT 1
                FROM sys.friendships
                WHERE user_low_id = @low AND user_high_id = @high
                    AND status = 'ACCEPTED'
                LIMIT 1;";

            await using (var cmd = new MySqlCommand(friendshipCheck, conn, mysqlTx))
            {
                cmd.Parameters.AddWithValue("@low", low);
                cmd.Parameters.AddWithValue("@high", high);

                var ok = await cmd.ExecuteScalarAsync();
                if (ok is null)
                    throw new InvalidOperationException("You can only start a chat with an accepted friend.");
            }

            // 2) Check if DM already exists
            const string existingDm = @"
                SELECT conversation_id
                FROM sys.direct_message_pairs
                WHERE user_low_id = @low AND user_high_id = @high
                LIMIT 1;";

            await using (var cmd = new MySqlCommand(existingDm, conn, mysqlTx))
            {
                cmd.Parameters.AddWithValue("@low", low);
                cmd.Parameters.AddWithValue("@high", high);

                var existing = await cmd.ExecuteScalarAsync();
                if (existing != null)
                {
                    await tx.CommitAsync();
                    return existing.ToString()!;
                }
            }

            // 3) Create new conversation
            var conversationId = Guid.NewGuid().ToString();

            const string insertConversation = @"
                INSERT INTO sys.conversations
                (
                    conversation_id,
                    conversation_type,
                    created_by_user_id,
                    title,
                    avatar_url,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @id,
                    'DM',
                    @me,
                    NULL,
                    NULL,
                    NOW(),
                    NOW()
                );";

            await using (var cmd = new MySqlCommand(insertConversation, conn, mysqlTx))
            {
                cmd.Parameters.AddWithValue("@id", conversationId);
                cmd.Parameters.AddWithValue("@me", meUserId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 4) Add both members
            const string insertMembers = @"
                INSERT INTO sys.conversation_members
                (
                    conversation_id,
                    user_id,
                    role,
                    joined_at,
                    left_at,
                    is_muted
                )
                VALUES
                (
                    @id,
                    @me,
                    'MEMBER',
                    NOW(),
                    NULL,
                    0
                ),
                (
                    @id,
                    @other,
                    'MEMBER',
                    NOW(),
                    NULL,
                    0
                );";

            await using (var cmd = new MySqlCommand(insertMembers, conn, mysqlTx))
            {
                cmd.Parameters.AddWithValue("@id", conversationId);
                cmd.Parameters.AddWithValue("@me", meUserId);
                cmd.Parameters.AddWithValue("@other", otherUserId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 5) Insert DM pair unique record
            const string insertPair = @"
                INSERT INTO sys.direct_message_pairs (conversation_id, user_low_id, user_high_id)
                VALUES(@id, @low, @high);
                ";

            await using (var cmd = new MySqlCommand(insertPair, conn, mysqlTx))
            {
                cmd.Parameters.AddWithValue("@id", conversationId);
                cmd.Parameters.AddWithValue("@low", low);
                cmd.Parameters.AddWithValue("@high", high);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return conversationId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<ConversationListItem>> GetAllForUserAsync(string userId)
    {
        // NOTE:
        // For DM: cm2 joins the "other member" (user != @me).
        // For GROUP: this query will produce multiple rows (one per other member).
        // If you're only doing DMs for now, you're good.
        const string sql = @"
            SELECT
            c.conversation_id,
            c.conversation_type,
            c.title,
            c.avatar_url,

            u.user_id     AS other_user_id,
            u.username    AS other_username,
            u.first_name  AS other_first_name,
            u.last_name   AS other_last_name,

            lm.last_message_at,
            lm.last_message_preview

            FROM sys.conversation_members cm
            JOIN sys.conversations c
            ON c.conversation_id = cm.conversation_id

            LEFT JOIN sys.conversation_members cm2
            ON cm2.conversation_id = c.conversation_id
            AND cm2.user_id <> @me
            AND cm2.left_at IS NULL

            LEFT JOIN sys.users u
            ON u.user_id = cm2.user_id

            LEFT JOIN (
            SELECT
                m.conversation_id,
                MAX(m.sent_at) AS last_message_at,
                SUBSTRING_INDEX(
                GROUP_CONCAT(m.ciphertext ORDER BY m.sent_at DESC SEPARATOR '|||'),
                '|||',
                1
                ) AS last_message_preview
            FROM sys.messages m
            WHERE m.deleted_at IS NULL
            GROUP BY m.conversation_id
            ) lm
            ON lm.conversation_id = c.conversation_id

            WHERE cm.user_id = @me
            AND cm.left_at IS NULL

            ORDER BY COALESCE(lm.last_message_at, c.created_at) DESC;";

        var results = new List<ConversationListItem>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@me", userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var ordConversationId     = reader.GetOrdinal( "conversation_id"      );
            var ordConversationType   = reader.GetOrdinal( "conversation_type"    );
            var ordTitle              = reader.GetOrdinal( "title"                );
            var ordAvatarUrl          = reader.GetOrdinal( "avatar_url"           );
            var ordOtherUserId        = reader.GetOrdinal( "other_user_id"        );
            var ordOtherUsername      = reader.GetOrdinal( "other_username"       );
            var ordOtherFirstName     = reader.GetOrdinal( "other_first_name"     );
            var ordOtherLastName      = reader.GetOrdinal( "other_last_name"      );
            var ordLastMessageAt      = reader.GetOrdinal( "last_message_at"      );
            var ordLastMessagePreview = reader.GetOrdinal( "last_message_preview" );

            results.Add(
                new ConversationListItem
                {
                    ConversationId     = reader.GetString(ordConversationId),
                    ConversationType   = reader.GetString(ordConversationType),

                    Title              = reader.IsDBNull(ordTitle)              ? null : reader.GetString(ordTitle),
                    AvatarUrl          = reader.IsDBNull(ordAvatarUrl)          ? null : reader.GetString(ordAvatarUrl),
                    OtherUserId        = reader.IsDBNull(ordOtherUserId)        ? null : reader.GetString(ordOtherUserId),
                    OtherUsername      = reader.IsDBNull(ordOtherUsername)      ? null : reader.GetString(ordOtherUsername),
                    OtherFirstName     = reader.IsDBNull(ordOtherFirstName)     ? null : reader.GetString(ordOtherFirstName),
                    OtherLastName      = reader.IsDBNull(ordOtherLastName)      ? null : reader.GetString(ordOtherLastName),
                    LastMessagePreview = reader.IsDBNull(ordLastMessagePreview) ? null : reader.GetString(ordLastMessagePreview),
                    LastMessageAt      = reader.IsDBNull(ordLastMessageAt)      ? null : reader.GetDateTime(ordLastMessageAt),
                }
            );
        }

        return results;
    }

    private static (string low, string high) OrderPair(string a, string b)
        => string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
}