using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SudokuServer
{
    public class ClientSession
    {
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        private byte[] _buffer = new byte[2048];
        private StringBuilder _stringBuilder = new StringBuilder();
        private GameServer _server;

        public ClientSession(TcpClient tcpClient, GameServer server)
        {
            TcpClient = tcpClient;
            Stream = tcpClient.GetStream();
            _server = server;
        }

        public void StartReceiving()
        {
            try
            {
                Stream.BeginRead(_buffer, 0, _buffer.Length, OnReadComplete, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi khởi động đọc dữ liệu]: {ex.Message}");
                Disconnect();
            }
        }

        private void OnReadComplete(IAsyncResult ar)
        {
            try
            {
                int bytesRead = Stream.EndRead(ar);
                if (bytesRead <= 0)
                {
                    Disconnect();
                    return;
                }

                // Chuyển dữ liệu nhận được thành chuỗi và đưa vào StringBuilder
                string receivedText = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                _stringBuilder.Append(receivedText);

                // Xử lý Message Framing dựa trên ký tự xuống dòng '\n'
                string accumulated = _stringBuilder.ToString();
                int newlineIndex;
                while ((newlineIndex = accumulated.IndexOf('\n')) >= 0)
                {
                    string jsonMessage = accumulated.Substring(0, newlineIndex).Trim();
                    accumulated = accumulated.Substring(newlineIndex + 1);
                    _stringBuilder.Clear();
                    _stringBuilder.Append(accumulated);

                    if (!string.IsNullOrEmpty(jsonMessage))
                    {
                        ProcessMessage(jsonMessage);
                    }
                }

                // Tiếp tục lắng nghe dữ liệu tiếp theo từ Client này
                Stream.BeginRead(_buffer, 0, _buffer.Length, OnReadComplete, null);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private void ProcessMessage(string jsonMessage)
        {
            try
            {
                var request = JsonSerializer.Deserialize<RequestMessage>(jsonMessage);
                if (request != null)
                {
                    _server.HandleClientRequest(this, request);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi phân tích JSON]: {ex.Message}");
            }
        }

        public void SendMessage(ResponseMessage response)
        {
            try
            {
                string json = JsonSerializer.Serialize(response) + "\n"; // Đảm bảo có \n ở cuối
                byte[] data = Encoding.UTF8.GetBytes(json);
                Stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi gửi tin tới Client]: {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            try
            {
                Stream?.Close();
                TcpClient?.Close();
                _server.RemoveClient(this);
                Console.WriteLine("[Thông báo] Một Client đã ngắt kết nối và được giải phóng tài nguyên.");
            }
            catch { }
        }
    }
}