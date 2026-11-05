
namespace SudokuShared.Messages;

public sealed class RequestMessage
{
    public string Type { get; set; } = string.Empty;

    // Payload chứa JSON được chuyển thành chuỗi.
    public string Payload { get; set; } = "{}";
}
