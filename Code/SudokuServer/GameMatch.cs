using SudokuLogic;
using SudokuShared.Messages;

namespace SudokuServer;

public sealed class MatchPlayer
{
    public string Name { get; }
    public ClientSession? Session { get; set; }
    public PlayerBoard Board { get; }
    public bool Connected { get; set; } = true;
    public int DisconnectLeft { get; set; }

    public MatchPlayer(string name, ClientSession? session, SudokuPuzzle puzzle)
    {
        Name = name; Session = session; Board = new PlayerBoard(puzzle);
    }
}

/// <summary>
/// Một trận đấu. Mọi phương thức public phải được gọi khi đang giữ lock của GameServer
/// (không có await bên trong); tin nhắn được đưa vào Outbox để gửi sau.
/// </summary>
public sealed class GameMatch
{
    public string Id { get; }
    public SudokuPuzzle Puzzle { get; }
    public MatchPlayer[] Players { get; }
    public List<ClientSession> Spectators { get; } = new();
    public int TimeLeft { get; private set; } = ProtocolConstants.MatchSeconds;
    public bool IsFinished { get; private set; }
    public string? Winner { get; private set; }
    public string Reason { get; private set; } = "";
    public DateTime StartedUtc { get; private set; } = DateTime.UtcNow;
    public DateTime EndedUtc { get; private set; }

    /// <summary>Được gọi (trong lock) khi trận kết thúc, để server dọn dẹp và lưu lịch sử.</summary>
    public Action<GameMatch>? OnFinished { get; set; }

    public GameMatch(string id, SudokuPuzzle puzzle, (string Name, ClientSession? Session) a, (string Name, ClientSession? Session) b)
    {
        Id = id;
        Puzzle = puzzle;
        Players = new[]
        {
            new MatchPlayer(a.Name, a.Session, puzzle),
            new MatchPlayer(b.Name, b.Session, puzzle)
        };
    }

    public bool IsPaused => !IsFinished && Players.Any(p => !p.Connected);
    public MatchPlayer? Find(string name) => Players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public MatchPlayer Other(MatchPlayer p) => Players[0] == p ? Players[1] : Players[0];

    private string StateText => IsFinished ? MatchStates.Finished : IsPaused ? MatchStates.Paused : MatchStates.Playing;
    private int PauseLeft => Players.Where(p => !p.Connected).Select(p => p.DisconnectLeft).DefaultIfEmpty(0).Min();

    // ---------- Snapshot ----------
    /// <summary>forName = tên người chơi nhận (chỉ thấy bảng của mình); null = khán giả/kết thúc (thấy cả hai).</summary>
    public MatchSnapshot Snapshot(string? forName) => new()
    {
        MatchId = Id,
        You = forName ?? "",
        Puzzle = Puzzle.Puzzle,
        TimeLeft = TimeLeft,
        State = StateText,
        PauseLeft = PauseLeft,
        Players = Players.Select(p => new PlayerSnapshot
        {
            Name = p.Name,
            Board = forName == null || p.Name.Equals(forName, StringComparison.OrdinalIgnoreCase)
                ? SudokuGenerator.Clone(p.Board.Cells) : null,
            Score = p.Board.Score,
            Filled = p.Board.CorrectCount,
            Total = p.Board.Total,
            Connected = p.Connected
        }).ToArray()
    };

    public MatchSummary Summary() => new()
    {
        MatchId = Id,
        Player1 = Players[0].Name, Player2 = Players[1].Name,
        Score1 = Players[0].Board.Score, Score2 = Players[1].Board.Score,
        TimeLeft = TimeLeft, State = StateText
    };

    // ---------- Vòng đời ----------
    public void Start(Outbox o)
    {
        StartedUtc = DateTime.UtcNow;
        foreach (var p in Players)
            o.Add(p.Session, Msg.Make(MessageTypes.MatchFound, $"Tìm thấy đối thủ: {Other(p).Name}",
                new MatchFoundData { MatchId = Id, Opponent = Other(p).Name }));
        foreach (var p in Players)
            o.Add(p.Session, Msg.Make(MessageTypes.GameStarted, "Trận đấu bắt đầu", Snapshot(p.Name)));
    }

    public void Submit(MatchPlayer p, MovePayload mv, Outbox o)
    {
        if (IsFinished) { o.Add(p.Session, Msg.Error("Trận đấu đã kết thúc")); return; }
        if (IsPaused) { o.Add(p.Session, Msg.Error("Trận đang tạm dừng do có người mất kết nối")); return; }

        var r = p.Board.TryPlace(mv.Row, mv.Col, mv.Value);
        string reason = r.Outcome switch
        {
            MoveOutcome.Correct => "Đúng: +1 điểm",
            MoveOutcome.Wrong => "Sai: -1 điểm (không ghi vào bảng)",
            MoveOutcome.Locked => "Ô này không được sửa",
            _ => "Nước đi không hợp lệ"
        };
        o.Add(p.Session, Msg.Make(MessageTypes.MoveResult, reason, new MoveResultData
        {
            Row = mv.Row, Col = mv.Col, Value = mv.Value,
            Accepted = r.Outcome == MoveOutcome.Correct,
            ScoreDelta = r.ScoreDelta, Score = p.Board.Score, Reason = reason
        }));

        if (r.Outcome is MoveOutcome.Correct or MoveOutcome.Wrong)
        {
            BroadcastBoards(o);
            if (r.Outcome == MoveOutcome.Correct && p.Board.IsComplete)
                Finish(o, p.Name, $"{p.Name} hoàn thành bảng Sudoku trước");
        }
    }

    public void Surrender(MatchPlayer p, Outbox o)
    {
        if (IsFinished) return;
        Finish(o, Other(p).Name, $"{p.Name} bỏ cuộc");
    }

    public void PlayerDisconnected(MatchPlayer p, Outbox o)
    {
        if (IsFinished || !p.Connected) return;
        p.Connected = false;
        p.Session = null;
        p.DisconnectLeft = ProtocolConstants.ReconnectSeconds;
        BroadcastAll(o, Msg.Make(MessageTypes.GamePaused,
            $"{p.Name} mất kết nối. Tạm dừng tối đa {ProtocolConstants.ReconnectSeconds} giây",
            new TimeUpdateData { MatchId = Id, TimeLeft = TimeLeft, State = MatchStates.Paused, PauseLeft = p.DisconnectLeft, PausedPlayer = p.Name }));
    }

    public void PlayerReconnected(MatchPlayer p, ClientSession? session, Outbox o)
    {
        if (IsFinished) return;
        p.Connected = true;
        p.Session = session;
        p.DisconnectLeft = 0;
        o.Add(session, Msg.Make(MessageTypes.BoardUpdate, "Đã quay lại trận đấu", Snapshot(p.Name)));
        if (!IsPaused)
            BroadcastAll(o, Msg.Make(MessageTypes.GameResumed, $"{p.Name} đã quay lại. Trận đấu tiếp tục",
                new TimeUpdateData { MatchId = Id, TimeLeft = TimeLeft, State = MatchStates.Playing }));
    }

    public void Tick(Outbox o)
    {
        if (IsFinished) return;

        if (IsPaused)
        {
            foreach (var p in Players.Where(x => !x.Connected)) p.DisconnectLeft--;
            var expired = Players.Where(x => !x.Connected && x.DisconnectLeft <= 0).ToList();
            if (expired.Count == 2) { Finish(o, null, "Cả hai người chơi mất kết nối quá 1 phút"); return; }
            if (expired.Count == 1)
            {
                var loser = expired[0];
                Finish(o, Other(loser).Name, $"{loser.Name} mất kết nối quá {ProtocolConstants.ReconnectSeconds} giây (xử thua)");
                return;
            }
        }
        else
        {
            TimeLeft--;
            if (TimeLeft <= 0)
            {
                TimeLeft = 0;
                int s0 = Players[0].Board.Score, s1 = Players[1].Board.Score;
                if (s0 == s1) Finish(o, null, "Hết giờ, điểm bằng nhau");
                else Finish(o, (s0 > s1 ? Players[0] : Players[1]).Name, "Hết giờ, người điểm cao hơn thắng");
                return;
            }
        }

        var paused = Players.FirstOrDefault(x => !x.Connected);
        BroadcastAll(o, Msg.Make(MessageTypes.TimeUpdate, "", new TimeUpdateData
        {
            MatchId = Id, TimeLeft = TimeLeft, State = StateText, PauseLeft = PauseLeft, PausedPlayer = paused?.Name ?? ""
        }));
    }

    private void Finish(Outbox o, string? winner, string reason)
    {
        if (IsFinished) return;
        IsFinished = true;
        EndedUtc = DateTime.UtcNow;
        Winner = winner;
        Reason = reason;

        var final = Snapshot(null);
        foreach (var p in Players)
        {
            string result = winner == null ? "DRAW" : p.Name == winner ? "WIN" : "LOSE";
            o.Add(p.Session, Msg.Make(MessageTypes.GameOver, reason,
                new GameOverData { MatchId = Id, Winner = winner, Result = result, Reason = reason, Final = final }));
        }
        foreach (var s in Spectators)
            o.Add(s, Msg.Make(MessageTypes.GameOver, reason,
                new GameOverData { MatchId = Id, Winner = winner, Result = "END", Reason = reason, Final = final }));

        OnFinished?.Invoke(this);
    }

    // ---------- Gửi tin ----------
    private void BroadcastBoards(Outbox o)
    {
        foreach (var p in Players)
            o.Add(p.Session, Msg.Make(MessageTypes.BoardUpdate, "", Snapshot(p.Name)));
        var spec = Snapshot(null);
        foreach (var s in Spectators)
            o.Add(s, Msg.Make(MessageTypes.SpectatorUpdate, "", spec));
    }

    private void BroadcastAll(Outbox o, ResponseMessage m)
    {
        foreach (var p in Players) o.Add(p.Session, m);
        foreach (var s in Spectators) o.Add(s, m);
    }

    public void SendSpectatorSnapshot(ClientSession s, Outbox o)
        => o.Add(s, Msg.Make(MessageTypes.SpectatorUpdate, "Đang xem trận " + Id, Snapshot(null)));
}