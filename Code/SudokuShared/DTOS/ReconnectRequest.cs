
namespace SudokuShared.DTOs;

public sealed class ReconnectRequest
{
    public string PlayerId { get; set; } = string.Empty;

    public string ReconnectToken { get; set; } = string.Empty;

    public string MatchId { get; set; } = string.Empty;
}
