using SudokuServer;
using SudokuShared.Messages;

// Cấu hình cổng: tham số dòng lệnh > biến môi trường SUDOKU_PORT > 8888
int port = ProtocolConstants.DefaultPort;
if (args.Length > 0 && int.TryParse(args[0], out int p1)) port = p1;
else if (int.TryParse(Environment.GetEnvironmentVariable("SUDOKU_PORT"), out int p2)) port = p2;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine($"Sudoku Server (UDM_17) - cổng {port}. Nhấn Ctrl+C để dừng.");
await new GameServer().RunAsync(port, cts.Token);