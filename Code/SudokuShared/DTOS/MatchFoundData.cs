
namespace SudokuShared.DTOs;

public sealed class MatchFoundData
{
	public string MatchId { get; set; } = string.Empty;

	public string Player1Id { get; set; } = string.Empty;

	public string Player2Id { get; set; } = string.Empty;

	public int[][]? Puzzle { get; set; }

	public int DurationSeconds { get; set; } = 600;
}
