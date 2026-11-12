namespace SudokuShared.Messages;

/// <summary>Client gửi lên Server.</summary>
public class RequestMessage
{
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
}
