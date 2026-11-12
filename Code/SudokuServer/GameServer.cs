using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using SudokuLogic;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuServer;

public enum SessionState { Connected, Idle, InQueue, Playing, Spectating }

public class GameServer
{
    private TcpListener? _listener;
    private int _port;
    private bool _isRunning;

    private readonly List<ClientSession> _clients = new();
    private readonly Queue<ClientSession> _joinQueue = new();
    private readonly Dictionary<string, GameMatch> _matches = new();
    private readonly object _lock = new();
    private int _matchCounter;
    private CancellationTokenSource? _cts;

    public int Holes { get; set; } = 40;

    public GameServer(int port)
    {
        _port = port;
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _isRunning = true;
        _cts = new CancellationTokenSource();
        Logger.Info($"Server lắng nghe tại cổng {_port}");

        _ = AcceptClientsAsync();
        _ = TickLoopAsync(_cts.Token);
    }

    private async Task AcceptClientsAsync()
    {
        while (_isRunning && _listener != null)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync();
                Logger.Info($"Kết nối mới từ {tcpClient.Client.RemoteEndPoint}");

                var session = new ClientSession(tcpClient, this);
                lock (_lock)
                {
                    _clients.Add(session);
                }
                session.StartReceiving();
            }
            catch (Exception ex)
            {
                if (!_isRunning) break;
                Logger.Error($"Lỗi chấp nhận kết nối: {ex.Message}");
            }
        }
    }

    private async Task TickLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var o = new Outbox();
                try
                {
                    lock (_lock)
                    {
                        foreach (var m in _matches.Values.ToList()) m.Tick(o);
                    }
                }
                catch (Exception ex) { Logger.Error("Tick lỗi: " + ex); }
                await o.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    public void HandleClientRequest(ClientSession session, RequestMessage request)
    {
        Logger.Info(HeaderInfo(session, request.Type));
        var o = new Outbox();

        lock (_lock)
        {
            switch (request.Type)
            {
                case MessageTypes.JoinQueue:
                    EnqueuePlayer(session, o);
                    break;
                case MessageTypes.LeaveQueue:
                    LeaveQueue(session, o);
                    break;
                default:
                    o.Add(session, Msg.Error($"Message không được hỗ trợ: {request.Type}"));
                    break;
            }
        }
        _ = o.FlushAsync();
    }

    private static string HeaderInfo(ClientSession s, string type) => 
        $"Nhận lệnh {type} từ {s.Name ?? s.Endpoint}";

    public void EnqueuePlayer(ClientSession session, Outbox o)
    {
        if (session.State != SessionState.Idle) 
        { 
            o.Add(session, Msg.Error("Bạn không ở trạng thái rảnh")); 
            return; 
        }

        if (!_joinQueue.Contains(session))
        {
            _joinQueue.Enqueue(session);
            session.State = SessionState.InQueue;
            Logger.Info($"Đã thêm {session.Name ?? session.Endpoint} vào hàng chờ. Sĩ số chờ: {_joinQueue.Count}");
            o.Add(session, Msg.Make(MessageTypes.Waiting, "Đang chờ đối thủ..."));

            CheckAndMatchPlayers(o);
        }
        else
        {
            o.Add(session, Msg.Error("Bạn đã có sẵn trong hàng chờ rồi"));
        }
    }

    private void LeaveQueue(ClientSession session, Outbox o)
    {
        if (session.State != SessionState.InQueue)
        {
            o.Add(session, Msg.Error("Bạn không ở trong hàng chờ"));
            return;
        }
        
        var temp = new List<ClientSession>(_joinQueue);
        temp.Remove(session);
        _joinQueue.Clear();
        foreach (var s in temp) _joinQueue.Enqueue(s);

        session.State = SessionState.Idle;
        o.Add(session, Msg.Make(MessageTypes.Ok, "Đã rời hàng chờ"));
    }

    private void CheckAndMatchPlayers(Outbox o)
    {
        while (_joinQueue.Count >= 2)
        {
            var a = _joinQueue.Dequeue();
            var b = _joinQueue.Dequeue();

            StartMatch(a, b, o);
        }
    }

    private void StartMatch(ClientSession a, ClientSession b, Outbox o)
    {
        var puzzle = SudokuGenerator.Generate(Holes);
        string id = $"M{++_matchCounter:D4}";
        var m = new GameMatch(id, puzzle, (a.Name ?? a.Endpoint, a), (b.Name ?? b.Endpoint, b)) 
        { 
            OnFinished = OnMatchFinished 
        };
        
        _matches[id] = m;
        foreach (var x in new[] { a, b })
        {
            x.State = SessionState.Playing;
            x.Match = m;
        }
        Logger.Info($"Bắt đầu trận {id}: {a.Endpoint} vs {b.Endpoint}");
        m.Start(o);
    }

    private void OnMatchFinished(GameMatch m)
    {
        _matches.Remove(m.Id);
        foreach (var p in m.Players)
        {
            if (p.Session != null && p.Session.Match == m) 
            { 
                p.Session.State = SessionState.Idle; 
                p.Session.Match = null; 
            }
        }
        Logger.Info($"Kết thúc trận {m.Id}: thắng={m.Winner ?? "hòa"}; {m.Reason}");
    }

    public void RemoveClient(ClientSession session)
    {
        lock (_lock)
        {
            if (_clients.Contains(session))
            {
                _clients.Remove(session);
            }

            var temp = new List<ClientSession>(_joinQueue);
            if (temp.Contains(session))
            {
                temp.Remove(session);
                _joinQueue.Clear();
                foreach (var s in temp) _joinQueue.Enqueue(s);
            }

            if (session.State == SessionState.Playing && session.Match != null)
            {
                var o = new Outbox();
                var p = session.Match.Find(session.Name ?? "");
                if (p != null) session.Match.PlayerDisconnected(p, o);
                _ = o.FlushAsync();
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _listener?.Stop();
        Logger.Info("Server đã dừng hoạt động.");
    }
}