using System.Net.Sockets;
using System.Text;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuClient;

/// <summary>Kết nối TCP bất đồng bộ tới server; không bao giờ chặn luồng giao diện.</summary>
public sealed class NetworkClient : IDisposable
{
    private static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(10);

    private readonly object _gate = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpClient? _tcp;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    /// <summary>Gọi từ luồng nền: nhớ marshal về UI thread.</summary>
    public event Action<ResponseMessage>? MessageReceived;
    public event Action<string>? Disconnected;

    public bool IsConnected { get { lock (_gate) return _cts != null; } }

    public async Task ConnectAsync(string host, int port, TimeSpan timeout)
    {
        Teardown(null);
        var tcp = new TcpClient { NoDelay = true };
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await tcp.ConnectAsync(host, port, cts.Token);
        }
        catch (OperationCanceledException)
        {
            tcp.Dispose();
            throw new TimeoutException("Hết thời gian chờ kết nối");
        }
        catch
        {
            tcp.Dispose();
            throw;
        }

        var stream = tcp.GetStream();
        var utf8 = new UTF8Encoding(false);
        var reader = new StreamReader(stream, utf8);
        var token = new CancellationTokenSource();
        lock (_gate)
        {
            _tcp = tcp;
            _writer = new StreamWriter(stream, utf8) { AutoFlush = false };
            _cts = token;
        }
        _ = Task.Run(() => ReadLoopAsync(reader, token));
        _ = Task.Run(() => PingLoopAsync(token.Token));
    }

    public Task SendAsync(string type, object? payload = null)
    {
        var req = new RequestMessage
        {
            Type = type,
            Payload = payload == null ? "" : JsonHelper.Serialize(payload)
        };
        return SendRawAsync(JsonHelper.Serialize(req) + "\n");
    }

    private async Task SendRawAsync(string line)
    {
        StreamWriter? w; CancellationTokenSource? c;
        lock (_gate) { w = _writer; c = _cts; }
        if (w == null || c == null) throw new InvalidOperationException("Chưa kết nối tới server");
        try
        {
            await _sendLock.WaitAsync(c.Token);
            try
            {
                using var t = CancellationTokenSource.CreateLinkedTokenSource(c.Token);
                t.CancelAfter(TimeSpan.FromSeconds(5));
                await w.WriteAsync(line.AsMemory(), t.Token);
                await w.FlushAsync(t.Token);
            }
            finally { _sendLock.Release(); }
        }
        catch (Exception ex)
        {
            Teardown("Lỗi gửi dữ liệu: " + ex.Message);
            throw;
        }
    }

    private async Task ReadLoopAsync(StreamReader reader, CancellationTokenSource owner)
    {
        try
        {
            while (!owner.IsCancellationRequested)
            {
                using var t = CancellationTokenSource.CreateLinkedTokenSource(owner.Token);
                t.CancelAfter(ReadTimeout);
                string? line = await reader.ReadLineAsync(t.Token);
                if (line == null) { Teardown("Server đã đóng kết nối", owner); return; }
                if (string.IsNullOrWhiteSpace(line)) continue;

                ResponseMessage? msg = null;
                try { msg = JsonHelper.Deserialize<ResponseMessage>(line); }
                catch (System.Text.Json.JsonException) { /* bỏ qua message hỏng */ }
                if (msg != null) MessageReceived?.Invoke(msg);
            }
        }
        catch (OperationCanceledException)
        {
            if (!owner.IsCancellationRequested) Teardown("Quá thời gian chờ phản hồi từ server", owner);
        }
        catch (Exception ex) { Teardown(ex.Message, owner); }
    }

    private async Task PingLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(PingInterval, token);
                await SendAsync(MessageTypes.Ping);
            }
        }
        catch { /* đã xử lý trong Teardown */ }
    }

    /// <summary>Đóng kết nối. reason == null: do người dùng chủ động (không bắn sự kiện Disconnected).</summary>
    private void Teardown(string? reason, CancellationTokenSource? only = null)
    {
        CancellationTokenSource? c; TcpClient? t;
        lock (_gate)
        {
            if (only != null && _cts != only) return;
            c = _cts; t = _tcp;
            _cts = null; _tcp = null; _writer = null;
        }
        if (c == null) return;
        try { c.Cancel(); } catch { }
        try { t?.Close(); } catch { }
        if (reason != null) Disconnected?.Invoke(reason);
    }

    public void Disconnect() => Teardown(null);
    public void Dispose() => Teardown(null);
}