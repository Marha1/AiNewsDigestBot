using System.Collections.Concurrent;

namespace AiNewsDigestBot.Host.Shared.Services;

/// <summary>
///     Сервис для отслеживания состояния ожидания поискового запроса
/// </summary>
public class SearchStateService
{
    private readonly ConcurrentDictionary<long, bool> _waitingForSearch = new();

    /// <summary>
    ///     Проверить, ждёт ли пользователь ввода поискового запроса
    /// </summary>
    public Task<bool> IsWaitingForSearchAsync(long chatId)
    {
        return Task.FromResult(_waitingForSearch.ContainsKey(chatId) && _waitingForSearch[chatId]);
    }

    /// <summary>
    ///     Установить состояние ожидания поискового запроса
    /// </summary>
    public Task SetWaitingForSearchAsync(long chatId, bool isWaiting)
    {
        if (isWaiting)
            _waitingForSearch[chatId] = true;
        else
            _waitingForSearch.TryRemove(chatId, out _);

        return Task.CompletedTask;
    }
}