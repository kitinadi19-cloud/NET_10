using System.Net.Sockets;
using System.Text;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuServer;

<<<<<<< HEAD
=======
public enum SessionState { Connected, Idle, InQueue, Playing, Spectating }

>>>>>>> main
public sealed class ClientSession
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(45);
    public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

    private readonly TcpClient _tcp;
<<<<<<< HEAD
    private readonly NetworkStream _stream;
    private readonly byte[] _buffer = new byte[2048];
    private readonly StringBuilder _stringBuilder = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly GameServer _server;
=======
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
>>>>>>> main
    private volatile bool _closed;

    public string Endpoint { get; }
    public string? Name { get; set; }
    public SessionState State { get; set; } = SessionState.Connected;
    public GameMatch? Match { get; set; }
    public bool IsClosed => _closed;

<<<<<<< HEAD
    public ClientSession(TcpClient tcp, GameServer server)
    {
<<<<<<< HEAD
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; } 
        private byte[] _buffer = new byte[2048];
        private StringBuilder _stringBuilder = new StringBuilder();
        private GameServer _server;
=======
        _tcp = tcp;
        _tcp.NoDelay = true;
        _stream = tcp.GetStream();
        _server = server;
        Endpoint = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
    }
>>>>>>> 53348cf (chinh sua sever)

    public void StartReceiving()
    {
        try
        {
            _stream.BeginRead(_buffer, 0, _buffer.Length, OnReadComplete, null);
        }
        catch
        {
            Disconnect();
        }
    }

    private void OnReadComplete(IAsyncResult ar)
    {
        try
        {
            int bytesRead = _stream.EndRead(ar);
            if (bytesRead <= 0)
            {
                Disconnect();
                return;
            }

            string receivedText = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
            _stringBuilder.Append(receivedText);

            string accumulated = _stringBuilder.ToString();
            int newlineIndex;
            while ((newlineIndex = accumulated.IndexOf('\n')) >= 0)
            {
                string jsonMessage = accumulated[..newlineIndex].Trim();
                accumulated = accumulated[(newlineIndex + 1)..];
                _stringBuilder.Clear();
                _stringBuilder.Append(accumulated);

                if (!string.IsNullOrEmpty(jsonMessage))
                {
                    ProcessMessage(jsonMessage);
                }
            }

            _stream.BeginRead(_buffer, 0, _buffer.Length, OnReadComplete, null);
        }
        catch
        {
            Disconnect();
        }
    }

    private void ProcessMessage(string jsonMessage)
    {
        try
        {
            var request = JsonHelper.Deserialize<RequestMessage>(jsonMessage);
            if (request != null && !string.IsNullOrEmpty(request.Type))
            {
                _server.HandleClientRequest(this, request);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Lỗi phân tích JSON từ {Endpoint}: {ex.Message}");
        }
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
                var data = Encoding.UTF8.GetBytes(line);
                using var cts = new CancellationTokenSource(SendTimeout);
                await _stream.WriteAsync(data.AsMemory(), cts.Token);
                await _stream.FlushAsync(cts.Token);
                return true;
            }
            finally { _sendLock.Release(); }
        }
        catch
        {
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        if (_closed) return;
        _closed = true;
        try 
        { 
            _stream?.Close();
            _tcp?.Close(); 
        } 
        catch { }
        _server.RemoveClient(this);
        Logger.Info($"Ngắt kết nối: {Name ?? Endpoint}");
    }
}
=======
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
>>>>>>> main
