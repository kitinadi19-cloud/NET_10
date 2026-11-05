
namespace SudokuShared.DTOs;

public sealed class BoardUpdateData
{
    public string MatchId { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public int[][]? Board { get; set; }

    public int Score { get; set; }
}
