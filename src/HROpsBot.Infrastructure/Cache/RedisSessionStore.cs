using System.Text.Json;
using HROpsBot.Core.Dialog;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HROpsBot.Infrastructure.Cache;

public class RedisSessionStore(IConnectionMultiplexer redis, ILogger<RedisSessionStore> logger)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan _ttl = TimeSpan.FromHours(2);
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static string Key(long chatId) => $"hrops:session:{chatId}";

    public async Task<ConversationState> GetOrCreateAsync(long chatId)
    {
        try
        {
            var json = await _db.StringGetAsync(Key(chatId));
            if (json.HasValue)
            {
                var state = JsonSerializer.Deserialize<ConversationState>(json!, _jsonOpts);
                if (state != null) return state;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis read failed for chatId={ChatId}, creating new state", chatId);
        }

        return new ConversationState { ChatId = chatId };
    }

    public async Task SaveAsync(ConversationState state)
    {
        try
        {
            state.LastActivity = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(state);
            await _db.StringSetAsync(Key(state.ChatId), json, _ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis write failed for chatId={ChatId}", state.ChatId);
        }
    }

    public async Task DeleteAsync(long chatId)
    {
        try { await _db.KeyDeleteAsync(Key(chatId)); }
        catch (Exception ex) { logger.LogWarning(ex, "Redis delete failed for chatId={ChatId}", chatId); }
    }
}
