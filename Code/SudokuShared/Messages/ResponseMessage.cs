
using System.Text.Json;

namespace SudokuShared.Messages;

public sealed class ResponseMessage
{
    public string Status { get; set; } = "SUCCESS";

    public string Message { get; set; } = string.Empty;

    public JsonElement? Data { get; set; }
}
