
namespace SudokuShared.DTOs;

public sealed class JoinQueueRequest
{
	public string PlayerId { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;
}
