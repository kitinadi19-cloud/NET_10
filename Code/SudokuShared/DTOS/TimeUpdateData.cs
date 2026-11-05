namespace SudokuShared.DTOs;

public sealed class TimeUpdateData
{
    public string MatchId { get; set; } = string.Empty;

    public int RemainingSeconds { get; set; }
}