namespace SudokuServer
{
    public class RequestMessage
    {
        public string Action { get; set; } = string.Empty; // Thuộc tính xác định loại lệnh
        public string Data { get; set; }   = string.Empty; // Dữ liệu đi kèm nếu có
    }

    public class ResponseMessage
    {
        public string Status { get; set; }  = string.Empty; // Trạng thái phản hồi (SUCCESS, ERROR)
        public string Message { get; set; } = string.Empty; // Nội dung thông báo
    }
}