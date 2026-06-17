using System.Collections.Concurrent;

namespace AiNewsDigestBot.Host.Shared.Services;

public class SettingsStateService
{
    private readonly ConcurrentDictionary<long, string> _states = new();

    public Task<string?> GetStateAsync(long chatId)
    {
        _states.TryGetValue(chatId, out var state);
        return Task.FromResult(state);
    }

    public Task SetStateAsync(long chatId, string? state)
    {
        if (string.IsNullOrEmpty(state))
            _states.TryRemove(chatId, out _);
        else
            _states[chatId] = state;

        return Task.CompletedTask;
    }
}