using System.Text.Json;

namespace SudokuShared.Messages;

/// <summary>
/// Server gửi về Client. Status chứa TÊN LOẠI message (WAITING, MATCH_FOUND, BOARD_UPDATE, ERROR...).
/// </summary>
public class ResponseMessage
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public JsonElement? Data { get; set; }
}