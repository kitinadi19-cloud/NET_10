using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SudokuServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 8888; // Cổng kết nối mặc định cho Server
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            
            try
            {
                listener.Start();
                Console.WriteLine($"[SERVER] Đang khởi động... Lắng nghe kết nối tại cổng {port}");

                while (true)
                {
                    // Chờ và chấp nhận kết nối từ Client bất đồng bộ (không làm đơ server)
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    
                    // Xử lý riêng cho từng Client kết nối vào bằng một Task độc lập
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientIp = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            Console.WriteLine($"[INFO] Client mới đã kết nối từ: {clientIp}");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                try
                {
                    // Đọc dữ liệu gửi lên từ Client liên tục
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        Console.WriteLine($"[RECEIVED từ {clientIp}] {message}");

                        // Phản hồi (Echo/Xử lý tạm thời) lại cho Client
                        string responseMessage = $"Server đã nhận tin nhắn: '{message}'\n";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Lỗi kết nối với client {clientIp}: {ex.Message}");
                }
            }

            Console.WriteLine($"[INFO] Client {clientIp} đã ngắt kết nối.");
        }
    }
}