
namespace SudokuShared.DTOs;

public sealed class GameOverData
{
    public string MatchId { get; set; } = string.Empty;

    public string WinnerId { get; set; } = string.Empty;

    public string LoserId { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public string Result { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
