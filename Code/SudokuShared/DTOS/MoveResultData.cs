
namespace SudokuShared.DTOs;

public sealed class MoveResultData
{
    public bool IsCorrect { get; set; }

    public int Row { get; set; }

    public int Col { get; set; }

    public int Value { get; set; }

    public int Score { get; set; }

    public string ResultCode { get; set; } = string.Empty;
}
