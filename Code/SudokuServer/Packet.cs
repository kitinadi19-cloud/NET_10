namespace SudokuServer
{
    // Cấu trúc chung cho mọi message từ Client gửi lên Server
    public class RequestMessage
    {
        public string Type { get; set; } = string.Empty; // Ví dụ: "JOIN_QUEUE", "SUBMIT_MOVE"
        public string Payload { get; set; } = string.Empty; // Dữ liệu chi tiết đi kèm
    }

    // Cấu trúc phản hồi từ Server về Client
    public class ResponseMessage
    {
        public string Status { get; set; } = "OK"; // "OK", "ERROR", "WAITING", "MATCH_FOUND"
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}