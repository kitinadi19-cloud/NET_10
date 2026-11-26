using System.Net.Sockets;
using System.Text;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuServer;

public enum SessionState { Connected, Idle, InQueue, Playing, Spectating }

public sealed class ClientSession
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(45);
    public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

    private readonly TcpClient _tcp;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private volatile bool _closed;

    public string Endpoint { get; }
    public string? Name { get; set; }
    public SessionState State { get; set; } = SessionState.Connected;
    public GameMatch? Match { get; set; }
    public bool IsClosed => _closed;

    public ClientSession(TcpClient tcp)
    {
        _tcp = tcp;
        _tcp.NoDelay = true;
        Endpoint = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
        var stream = tcp.GetStream();
        var utf8 = new UTF8Encoding(false);
        _reader = new StreamReader(stream, utf8);
        _writer = new StreamWriter(stream, utf8) { AutoFlush = false };
    }

    /// <summary>Đọc một dòng JSON. Ném OperationCanceledException nếu quá thời gian chờ.</summary>
    public async Task<string?> ReadLineAsync()
    {
        using var cts = new CancellationTokenSource(IdleTimeout);
        return await _reader.ReadLineAsync(cts.Token);
    }

    public async Task<bool> SendAsync(ResponseMessage msg)
    {
        if (_closed) return false;
        var line = JsonHelper.Serialize(msg) + "\n";
        try
        {
            await _sendLock.WaitAsync();
            try
            {
                using var cts = new CancellationTokenSource(SendTimeout);
                await _writer.WriteAsync(line.AsMemory(), cts.Token);
                await _writer.FlushAsync(cts.Token);
                return true;
            }
            finally { _sendLock.Release(); }
        }
        catch
        {
            Close();
            return false;
        }
    }

    public void Close()
    {
        if (_closed) return;
        _closed = true;
        try { _tcp.Close(); } catch { }
    }
}
