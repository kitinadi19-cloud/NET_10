namespace SudokuShared.Messages;

// ---------- Payload (Client -> Server) ----------
public class LoginPayload { public string Name { get; set; } = ""; }
public class ChallengePayload { public string Target { get; set; } = ""; }
public class ChallengeResponsePayload { public string Challenger { get; set; } = ""; public bool Accept { get; set; } }
public class MovePayload { public int Row { get; set; } public int Col { get; set; } public int Value { get; set; } }
public class SpectatePayload { public string MatchId { get; set; } = ""; }

// ---------- Data (Server -> Client) ----------
public class MatchFoundData { public string MatchId { get; set; } = ""; public string Opponent { get; set; } = ""; }
public class ChallengeData { public string From { get; set; } = ""; }

public class PlayerListItem { public string Name { get; set; } = ""; public string Status { get; set; } = ""; }

public class MatchSummary
{
    public string MatchId { get; set; } = "";
    public string Player1 { get; set; } = "";
    public string Player2 { get; set; } = "";
    public int Score1 { get; set; }
    public int Score2 { get; set; }
    public int TimeLeft { get; set; }
    public string State { get; set; } = "";
}

public class PlayerSnapshot
{
    public string Name { get; set; } = "";
    /// <summary>Bảng hiện tại. Null nếu người nhận là đối thủ (không được xem bảng của nhau).</summary>
    public int[][]? Board { get; set; }
    public int Score { get; set; }
    public int Filled { get; set; }
    public int Total { get; set; }
    public bool Connected { get; set; } = true;
}

public class MatchSnapshot
{
    public string MatchId { get; set; } = "";
    public string You { get; set; } = "";       // rỗng nếu là khán giả
    public int[][] Puzzle { get; set; } = Array.Empty<int[]>();
    public PlayerSnapshot[] Players { get; set; } = Array.Empty<PlayerSnapshot>();
    public int TimeLeft { get; set; }
    public string State { get; set; } = MatchStates.Playing;
    public int PauseLeft { get; set; }
}

public class MoveResultData
{
    public int Row { get; set; }
    public int Col { get; set; }
    public int Value { get; set; }
    public bool Accepted { get; set; }
    public int ScoreDelta { get; set; }
    public int Score { get; set; }
    public string Reason { get; set; } = "";
}

public class TimeUpdateData
{
    public string MatchId { get; set; } = "";
    public int TimeLeft { get; set; }
    public string State { get; set; } = MatchStates.Playing;
    public int PauseLeft { get; set; }
    public string PausedPlayer { get; set; } = "";
}

public class GameOverData
{
    public string MatchId { get; set; } = "";
    public string? Winner { get; set; }
    /// <summary>WIN / LOSE / DRAW (người chơi) hoặc END (khán giả).</summary>
    public string Result { get; set; } = "";
    public string Reason { get; set; } = "";
    public MatchSnapshot Final { get; set; } = new();
}
