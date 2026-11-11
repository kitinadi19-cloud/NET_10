using System.Text.Json; 
using SudokuShared;     

namespace SudokuServer; 

public class MatchRecord 
{
    // Gọi các lớp, hàm: mã trận, tên người chơi, thời điểm bắt đầu, kết thúc, kết quả...
    public string MatchId { get; set; } = "";     
    public string Player1 { get; set; } = "";     
    public string Player2 { get; set; } = "";     
    public int[][]? Puzzle { get; set; }          
    public int? Correct1 { get; set; }           
    public int? Correct2 { get; set; }            
    public int Score1 { get; set; }               
    public int Score2 { get; set; }               
    public DateTime StartedUtc { get; set; }      
    public DateTime EndedUtc { get; set; }        
    public int DurationSeconds { get; set; }      
    public string? Winner { get; set; }           
    public string Reason { get; set; } = "";      
}

public sealed class HistoryStore // Quản lý file lịch sử chung của server.
{
    private readonly object _gate = new(); // Cho từng lượt đọc hoặc ghi dùng file luân phiên.
    private readonly string _dir;          // Thư mục chứa dữ liệu.
    private readonly string _file;         // Đường dẫn đến file lịch sử.

    public HistoryStore(string? dir = null) // Có thể truyền thư mục riêng khi kiểm thử.
    {
        _dir = dir ?? Path.Combine(AppContext.BaseDirectory, "data"); // Mặc định dùng thư mục data cạnh chương trình.
        _file = Path.Combine(_dir, "match_history.jsonl");           // Ghép thư mục với tên file.
    }

    public void Append(MatchRecord rec) // Ghi nối tiếp một trận vào cuối file
    {
        try // Theo dõi lỗi trong lúc ghi
        {
            lock (_gate) // Chờ lượt sử dụng file qua đối tượng HistoryStore
            {
                Directory.CreateDirectory(_dir); // Tạo thư mục nếu chưa có
                string json = JsonHelper.Serialize(rec); // Đổi bản ghi thành một chuỗi JSON
                File.AppendAllText(_file, json + Environment.NewLine); // Ghi một trận trên một dòng
            }
        }
        catch (Exception ex) // Giữ cách xử lý lỗi ghi của server hiện tại
        {
            Logger.Error("Không ghi được lịch sử: " + ex.Message); // Ghi nguyên nhân vào log
        }
    }

    public List<MatchRecord> ReadAll() // Đọc các trận theo thứ tự đã ghi
    {
        var list = new List<MatchRecord>(); // Chuẩn bị danh sách kết quả
        lock (_gate) // Đọc sau khi lượt ghi đang diễn ra hoàn tất
        {
            if (!File.Exists(_file)) return list; 
            int lineNo = 0; // Đếm dòng để tìm lại dữ liệu gặp lỗi
            foreach (string line in File.ReadLines(_file)) // Đọc lần lượt từng dòng
            {
                lineNo++; 
                if (string.IsNullOrWhiteSpace(line)) continue; 
                try
                {
                    var rec = JsonHelper.Deserialize<MatchRecord>(line); 
                    if (rec != null && !string.IsNullOrWhiteSpace(rec.MatchId)) 
                        list.Add(rec); 
                    else
                        Logger.Warn($"Lịch sử dòng {lineNo} thiếu mã trận.");
                }
                catch (JsonException ex) 
                {
                    Logger.Warn($"Lịch sử dòng {lineNo}: {ex.Message}"); 
                }
            }
        }
        return list; 
    }
}
