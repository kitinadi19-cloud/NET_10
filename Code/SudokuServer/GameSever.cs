using System.Net;
using System.Net.Sockets;
using System.Text.Json;
<<<<<<< HEAD
=======
using System.Text.RegularExpressions;
>>>>>>> main
using SudokuLogic;
using SudokuShared;
using SudokuShared.Messages;

namespace SudokuServer;

<<<<<<< HEAD
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
=======
public sealed class GameServer
{
    private const int MaxLineLength = 64 * 1024;
    private static readonly Regex NameRegex = new(@"^[\p{L}\p{N}_\- ]{1,20}$", RegexOptions.Compiled);

    // Toàn bộ trạng thái dùng chung được bảo vệ bởi _lock; không await trong lock.
    private readonly object _lock = new();
    private readonly Dictionary<string, ClientSession> _online = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ClientSession> _queue = new();
    private readonly Dictionary<string, GameMatch> _matches = new();
    private readonly Dictionary<string, GameMatch> _playerMatch = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _challenges = new(StringComparer.OrdinalIgnoreCase); // "from>to"
    private readonly HistoryStore _history;
    private int _matchCounter;

    public int Holes { get; set; } = 40;

    public GameServer(HistoryStore? history = null) => _history = history ?? new HistoryStore();

    // ===================== Vòng lặp chính =====================
    public async Task RunAsync(int port, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Logger.Info($"Server lắng nghe tại cổng {port}");
        _ = TickLoopAsync(ct);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(tcp));
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
            Logger.Info("Server đã dừng");
>>>>>>> main
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
<<<<<<< HEAD
                    {
                        foreach (var m in _matches.Values.ToList()) m.Tick(o);
                    }
=======
                        foreach (var m in _matches.Values.ToList()) m.Tick(o);
>>>>>>> main
                }
                catch (Exception ex) { Logger.Error("Tick lỗi: " + ex); }
                await o.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

<<<<<<< HEAD
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
=======
    // ===================== Mỗi client =====================
    private async Task HandleClientAsync(TcpClient tcp)
    {
        ClientSession session;
        try { session = new ClientSession(tcp); }
        catch { tcp.Close(); return; }
        Logger.Info($"Kết nối mới từ {session.Endpoint}");

        try
        {
            while (true)
            {
                string? line = await session.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Length > MaxLineLength)
                {
                    await session.SendAsync(Msg.Error("Message quá dài"));
                    continue;
                }

                RequestMessage? req;
                try { req = JsonHelper.Deserialize<RequestMessage>(line); }
                catch (JsonException)
                {
                    await session.SendAsync(Msg.Error("JSON không hợp lệ"));
                    continue;
                }
                if (req == null || string.IsNullOrWhiteSpace(req.Type))
                {
                    await session.SendAsync(Msg.Error("Thiếu trường Type"));
                    continue;
                }

                try { await DispatchAsync(session, req); }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi xử lý {req.Type} từ {session.Name ?? session.Endpoint}: {ex}");
                    await session.SendAsync(Msg.Error("Lỗi nội bộ server"));
                }
            }
        }
        catch (OperationCanceledException) { Logger.Warn($"{session.Name ?? session.Endpoint}: quá thời gian không phản hồi"); }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { Logger.Error($"Lỗi kết nối {session.Endpoint}: {ex.Message}"); }
        finally
        {
            await CleanupAsync(session);
        }
    }

    private async Task CleanupAsync(ClientSession s)
    {
        s.Close();
        var o = new Outbox();
        lock (_lock)
        {
            if (s.Name != null && _online.TryGetValue(s.Name, out var cur) && cur == s)
                _online.Remove(s.Name);
            _queue.Remove(s);
            if (s.Name != null) RemoveChallengesOf(s.Name);

            if (s.State == SessionState.Playing && s.Match != null && s.Name != null)
            {
                var p = s.Match.Find(s.Name);
                if (p != null && p.Session == s) s.Match.PlayerDisconnected(p, o);
            }
            else if (s.State == SessionState.Spectating && s.Match != null)
            {
                s.Match.Spectators.Remove(s);
            }
            s.State = SessionState.Connected;
            s.Match = null;
        }
        Logger.Info($"Ngắt kết nối: {s.Name ?? s.Endpoint}");
        await o.FlushAsync();
    }

    // ===================== Xử lý yêu cầu =====================
    private async Task DispatchAsync(ClientSession s, RequestMessage req)
    {
        if (req.Type == MessageTypes.Ping)
        {
            await s.SendAsync(Msg.Make(MessageTypes.Pong));
            return;
        }
        if (req.Type == MessageTypes.Login)
        {
            await HandleLoginAsync(s, req);
            return;
        }
        if (s.Name == null)
        {
            await s.SendAsync(Msg.Error("Chưa đăng nhập. Hãy gửi LOGIN trước"));
            return;
        }

        var o = new Outbox();
        lock (_lock)
        {
            switch (req.Type)
            {
                case MessageTypes.JoinQueue: JoinQueue(s, o); break;
                case MessageTypes.LeaveQueue: LeaveQueue(s, o); break;
                case MessageTypes.SubmitMove: SubmitMove(s, req, o); break;
                case MessageTypes.Surrender: Surrender(s, o); break;
                case MessageTypes.ListPlayers: ListPlayers(s, o); break;
                case MessageTypes.ListMatches: ListMatches(s, o); break;
                case MessageTypes.Challenge: Challenge(s, req, o); break;
                case MessageTypes.ChallengeResponse: ChallengeResponse(s, req, o); break;
                case MessageTypes.SpectateMatch: Spectate(s, req, o); break;
                case MessageTypes.LeaveSpectate: LeaveSpectate(s, o); break;
                default: o.Add(s, Msg.Error($"Message không được hỗ trợ: {req.Type}")); break;
            }
        }
        await o.FlushAsync();
    }

    private static T? ReadPayload<T>(RequestMessage r) where T : class
    {
        try { return JsonHelper.ReadPayload<T>(r); }
        catch (Exception ex) when (ex is JsonException or ArgumentNullException) { return null; }
    }

    private async Task HandleLoginAsync(ClientSession s, RequestMessage req)
    {
        var o = new Outbox();
        var p = ReadPayload<LoginPayload>(req);
        string name = p?.Name?.Trim() ?? "";
        lock (_lock)
        {
            if (s.Name != null) { o.Add(s, Msg.Error("Bạn đã đăng nhập rồi")); }
            else if (!NameRegex.IsMatch(name)) { o.Add(s, Msg.Error("Tên không hợp lệ (1-20 ký tự chữ, số, _ - hoặc dấu cách)")); }
            else if (_online.ContainsKey(name)) { o.Add(s, Msg.Error("Tên này đang được sử dụng")); }
            else
            {
                s.Name = name;
                _online[name] = s;
                s.State = SessionState.Idle;
                Logger.Info($"Đăng nhập: {name} ({s.Endpoint})");
                o.Add(s, Msg.Make(MessageTypes.LoginOk, $"Xin chào {name}"));

                // Quay lại trận đang tạm dừng?
                if (_playerMatch.TryGetValue(name, out var match) && !match.IsFinished)
                {
                    var mp = match.Find(name);
                    if (mp != null && !mp.Connected)
                    {
                        s.State = SessionState.Playing;
                        s.Match = match;
                        match.PlayerReconnected(mp, s, o);
                        Logger.Info($"{name} quay lại trận {match.Id}");
                    }
                }
            }
        }
        await o.FlushAsync();
    }

    // ----- Hàng chờ -----
    private void JoinQueue(ClientSession s, Outbox o)
    {
        if (s.State != SessionState.Idle) { o.Add(s, Msg.Error("Bạn không ở trạng thái rảnh")); return; }
        RemoveChallengesOf(s.Name!);
        _queue.Add(s);
        s.State = SessionState.InQueue;
        if (_queue.Count >= 2)
        {
            var a = _queue[0]; var b = _queue[1];
            _queue.RemoveRange(0, 2);
            StartMatch(a, b, o);
        }
        else o.Add(s, Msg.Make(MessageTypes.Waiting, "Đang chờ đối thủ..."));
    }

    private void LeaveQueue(ClientSession s, Outbox o)
    {
        if (s.State != SessionState.InQueue) { o.Add(s, Msg.Error("Bạn không ở trong hàng chờ")); return; }
        _queue.Remove(s);
        s.State = SessionState.Idle;
        o.Add(s, Msg.Make(MessageTypes.Ok, "Đã rời hàng chờ"));
>>>>>>> main
    }

    private void StartMatch(ClientSession a, ClientSession b, Outbox o)
    {
        var puzzle = SudokuGenerator.Generate(Holes);
        string id = $"M{++_matchCounter:D4}";
<<<<<<< HEAD
        var m = new GameMatch(id, puzzle, (a.Name ?? a.Endpoint, a), (b.Name ?? b.Endpoint, b)) 
        { 
            OnFinished = OnMatchFinished 
        };
        
=======
        var m = new GameMatch(id, puzzle, (a.Name!, a), (b.Name!, b)) { OnFinished = OnMatchFinished };
>>>>>>> main
        _matches[id] = m;
        foreach (var x in new[] { a, b })
        {
            x.State = SessionState.Playing;
            x.Match = m;
<<<<<<< HEAD
        }
        Logger.Info($"Bắt đầu trận {id}: {a.Endpoint} vs {b.Endpoint}");
=======
            _playerMatch[x.Name!] = m;
        }
        Logger.Info($"Bắt đầu trận {id}: {a.Name} vs {b.Name}");
>>>>>>> main
        m.Start(o);
    }

    private void OnMatchFinished(GameMatch m)
    {
        _matches.Remove(m.Id);
        foreach (var p in m.Players)
        {
<<<<<<< HEAD
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
=======
            if (_playerMatch.TryGetValue(p.Name, out var cur) && cur == m) _playerMatch.Remove(p.Name);
            if (p.Session != null && p.Session.Match == m) { p.Session.State = SessionState.Idle; p.Session.Match = null; }
        }
        foreach (var sp in m.Spectators)
            if (sp.Match == m) { sp.State = SessionState.Idle; sp.Match = null; }
        m.Spectators.Clear();

        Logger.Info($"Kết thúc trận {m.Id}: thắng={m.Winner ?? "hòa"}; {m.Reason}");
        _history.Append(new MatchRecord
        {
            MatchId = m.Id,
            Player1 = m.Players[0].Name, Player2 = m.Players[1].Name,
            Score1 = m.Players[0].Board.Score, Score2 = m.Players[1].Board.Score,
            Winner = m.Winner, Reason = m.Reason,
            StartedUtc = m.StartedUtc, EndedUtc = m.EndedUtc,
            DurationSeconds = (int)(m.EndedUtc - m.StartedUtc).TotalSeconds
        });
    }

    // ----- Trong trận -----
    private void SubmitMove(ClientSession s, RequestMessage req, Outbox o)
    {
        if (s.State == SessionState.Spectating) { o.Add(s, Msg.Error("Khán giả không được gửi nước đi")); return; }
        if (s.State != SessionState.Playing || s.Match == null) { o.Add(s, Msg.Error("Bạn không ở trong trận đấu")); return; }
        var mv = ReadPayload<MovePayload>(req);
        if (mv == null) { o.Add(s, Msg.Error("Payload nước đi không hợp lệ")); return; }
        var p = s.Match.Find(s.Name!);
        if (p == null) { o.Add(s, Msg.Error("Bạn không phải người chơi của trận này")); return; }
        s.Match.Submit(p, mv, o);
    }

    private void Surrender(ClientSession s, Outbox o)
    {
        if (s.State != SessionState.Playing || s.Match == null) { o.Add(s, Msg.Error("Bạn không ở trong trận đấu")); return; }
        var p = s.Match.Find(s.Name!);
        if (p != null) s.Match.Surrender(p, o);
    }

    // ----- Danh sách -----
    private void ListPlayers(ClientSession s, Outbox o)
    {
        var list = _online.Values.Where(x => x.Name != null).Select(x => new PlayerListItem
        {
            Name = x.Name!,
            Status = x.State switch
            {
                SessionState.Idle => "Rảnh",
                SessionState.InQueue => "Đang chờ",
                SessionState.Playing => "Đang chơi",
                SessionState.Spectating => "Đang xem",
                _ => "-"
            }
        }).OrderBy(x => x.Name).ToList();
        o.Add(s, Msg.Make(MessageTypes.PlayerList, "", list));
    }

    private void ListMatches(ClientSession s, Outbox o)
        => o.Add(s, Msg.Make(MessageTypes.MatchList, "", _matches.Values.Select(m => m.Summary()).OrderBy(m => m.MatchId).ToList()));

    // ----- Thách đấu -----
    private void Challenge(ClientSession s, RequestMessage req, Outbox o)
    {
        var p = ReadPayload<ChallengePayload>(req);
        string target = p?.Target?.Trim() ?? "";
        if (s.State != SessionState.Idle) { o.Add(s, Msg.Error("Bạn đang bận")); return; }
        if (!_online.TryGetValue(target, out var t)) { o.Add(s, Msg.Error("Không tìm thấy người chơi")); return; }
        if (t == s) { o.Add(s, Msg.Error("Không thể tự thách đấu chính mình")); return; }
        if (t.State != SessionState.Idle) { o.Add(s, Msg.Error($"{t.Name} đang bận")); return; }

        _challenges.Add($"{s.Name}>{t.Name}");
        o.Add(t, Msg.Make(MessageTypes.ChallengeReceived, $"{s.Name} muốn thách đấu bạn", new ChallengeData { From = s.Name! }));
        o.Add(s, Msg.Make(MessageTypes.Ok, $"Đã gửi lời thách đấu tới {t.Name}"));
    }

    private void ChallengeResponse(ClientSession s, RequestMessage req, Outbox o)
    {
        var p = ReadPayload<ChallengeResponsePayload>(req);
        string fromName = p?.Challenger?.Trim() ?? "";
        if (p == null || !_challenges.Remove($"{fromName}>{s.Name}"))
        {
            o.Add(s, Msg.Error("Không có lời thách đấu này (có thể đã hết hạn)"));
            return;
        }
        _online.TryGetValue(fromName, out var challenger);

        if (!p.Accept)
        {
            o.Add(challenger, Msg.Make(MessageTypes.ChallengeDeclined, $"{s.Name} đã từ chối lời thách đấu"));
            return;
        }
        if (challenger == null || challenger.State != SessionState.Idle || s.State != SessionState.Idle)
        {
            o.Add(s, Msg.Error("Đối thủ không còn sẵn sàng"));
            return;
        }
        RemoveChallengesOf(s.Name!);
        RemoveChallengesOf(challenger.Name!);
        StartMatch(challenger, s, o);
    }

    private void RemoveChallengesOf(string name)
    {
        _challenges.RemoveWhere(k =>
        {
            var parts = k.Split('>');
            return parts[0].Equals(name, StringComparison.OrdinalIgnoreCase) || parts[1].Equals(name, StringComparison.OrdinalIgnoreCase);
        });
    }

    // ----- Khán giả -----
    private void Spectate(ClientSession s, RequestMessage req, Outbox o)
    {
        if (s.State != SessionState.Idle) { o.Add(s, Msg.Error("Bạn đang bận, không thể xem trận")); return; }
        var p = ReadPayload<SpectatePayload>(req);
        if (p == null || !_matches.TryGetValue(p.MatchId ?? "", out var m)) { o.Add(s, Msg.Error("Không tìm thấy trận đấu")); return; }
        m.Spectators.Add(s);
        s.State = SessionState.Spectating;
        s.Match = m;
        m.SendSpectatorSnapshot(s, o);
        Logger.Info($"{s.Name} vào xem trận {m.Id}");
    }

    private void LeaveSpectate(ClientSession s, Outbox o)
    {
        if (s.State != SessionState.Spectating || s.Match == null) { o.Add(s, Msg.Error("Bạn không đang xem trận nào")); return; }
        s.Match.Spectators.Remove(s);
        s.Match = null;
        s.State = SessionState.Idle;
        o.Add(s, Msg.Make(MessageTypes.Ok, "Đã rời phòng xem"));
    }
}
>>>>>>> main