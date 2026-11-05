
namespace SudokuShared.DTOs;

public sealed class SubmitMoveRequest
{
    public string MatchId { get; set; } = string.Empty;

    public int Row { get; set; }

    public int Col { get; set; }

    public int Value { get; set; }
}
