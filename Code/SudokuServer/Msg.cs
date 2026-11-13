using SudokuShared;
using SudokuShared.Messages;

namespace SudokuServer;

public static class Msg
{
    public static ResponseMessage Make(string status, string message = "", object? data = null)
        => JsonHelper.CreateResponse<object>(status, message, data);

    public static ResponseMessage Error(string message) => Make(MessageTypes.Error, message);
}

/// <summary>Gom các message cần gửi trong lúc giữ lock; gửi thật sự SAU khi đã nhả lock.</summary>
public sealed class Outbox
{
    private readonly List<(ClientSession Session, ResponseMessage Message)> _items = new();

    public int Count => _items.Count;

    public void Add(ClientSession? session, ResponseMessage message)
    {
        if (session != null) _items.Add((session, message));
    }

    public async Task FlushAsync()
    {
        if (_items.Count == 0) return;
        var tasks = _items.GroupBy(i => i.Session).Select(async g =>
        {
            foreach (var item in g) await item.Session.SendAsync(item.Message);
        }).ToList();
        _items.Clear();
        await Task.WhenAll(tasks);
    }
}