namespace SudokuServer;

/// <summary>Ghi log ra console và file logs/server.log (không ghi dữ liệu bí mật).</summary>
public static class Logger
{
    private static readonly object Gate = new();
    private static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "logs");
    public static bool Quiet { get; set; }

    public static void Info(string msg) => Write("INFO", msg);
    public static void Warn(string msg) => Write("WARN", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
        lock (Gate)
        {
            if (!Quiet) Console.WriteLine(line);
            try
            {
                Directory.CreateDirectory(Dir);
                File.AppendAllText(Path.Combine(Dir, "server.log"), line + Environment.NewLine);
            }
            catch { /* log lỗi không được làm sập server */ }
        }
    }
}
