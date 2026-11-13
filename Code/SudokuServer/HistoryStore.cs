using SudokuShared;

namespace SudokuServer;

public class MatchRecord
{
    public string MatchId { get; set; } = "";
    public string Player1 { get; set; } = "";
    public string Player2 { get; set; } = "";
    public int Score1 { get; set; }
    public int Score2 { get; set; }
    public string? Winner { get; set; }
    public string Reason { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public DateTime EndedUtc { get; set; }
    public int DurationSeconds { get; set; }
}

/// <summary>Lưu lịch sử trận đấu (mỗi dòng một JSON) vào data/match_history.jsonl.</summary>
public sealed class HistoryStore
{
    private readonly object _gate = new();
    private readonly string _file;

    public HistoryStore(string? dir = null)
    {
        dir ??= Path.Combine(AppContext.BaseDirectory, "data");
        _file = Path.Combine(dir, "match_history.jsonl");
    }

    public void Append(MatchRecord rec)
    {
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
                File.AppendAllText(_file, JsonHelper.Serialize(rec) + Environment.NewLine);
            }
        }
        catch (Exception ex) { Logger.Error("Không ghi được lịch sử: " + ex.Message); }
    }
}