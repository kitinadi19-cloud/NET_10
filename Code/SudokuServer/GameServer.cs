using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuServer
{
    public class GameServer
    {
        private TcpListener? _listener; // Đã thêm dấu ? để hỗ trợ nullable
        private int _port;
        private bool _isRunning;
        
        // Danh sách các client đang kết nối
        private readonly List<ClientSession> _clients = new List<ClientSession>();
        // Hàng chờ ghép trận
        private readonly Queue<ClientSession> _joinQueue = new Queue<ClientSession>();
        private readonly object _lock = new object();

        public GameServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[Server đang chạy] Đang lắng nghe cổng 0.0.0.0:{_port}...");

            _ = AcceptClientsAsync();
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("[Kết nối mới] Một Client vừa kết nối thành công!");

                    ClientSession session = new ClientSession(tcpClient, this);
                    
                    lock (_lock)
                    {
                        _clients.Add(session);
                    }

                    // Bắt đầu nhận dữ liệu từ client
                    session.StartReceiving();
                }
                catch (Exception ex)
                {
                    if (!_isRunning) break;
                    Console.WriteLine($"[Lỗi chấp nhận kết nối]: {ex.Message}");
                }
            }
        }

        // Xử lý yêu cầu gửi lên từ Client
        public void HandleClientRequest(ClientSession session, RequestMessage request)
        {
            Console.WriteLine($"[Nhận lệnh từ Client]: {request.Action}");

            // Xử lý tùy theo loại lệnh (Action) từ Protocol chung
            if (request.Action == "JOIN_QUEUE")
            {
                EnqueuePlayer(session);
            }
            // Các lệnh khác sẽ bổ sung sau...
        }

        // Đưa người chơi vào hàng chờ (Đảm bảo an toàn đa luồng & không lặp)
        public void EnqueuePlayer(ClientSession session)
        {
            lock (_lock)
            {
                if (!_joinQueue.Contains(session))
                {
                    _joinQueue.Enqueue(session);
                    Console.WriteLine($"[Hàng chờ] Đã thêm client vào hàng chờ. Sĩ số chờ: {_joinQueue.Count}");

                    // Kiểm tra đủ 2 người để ghép trận chưa
                    CheckAndMatchPlayers();
                }
                else
                {
                    Console.WriteLine("[Hàng chờ] Client này đã có sẵn trong hàng chờ rồi.");
                }
            }
        }

        private void CheckAndMatchPlayers()
        {
            while (_joinQueue.Count >= 2)
            {
                ClientSession player1 = _joinQueue.Dequeue();
                ClientSession player2 = _joinQueue.Dequeue();

                Console.WriteLine("[Ghép trận] Đã đủ 2 người chơi! Đang khởi tạo trận đấu...");
                // TODO: Tạo Match mới ở đây
            }
        }

        // Xử lý khi Client ngắt kết nối hoặc mất mạng
        public void RemoveClient(ClientSession session)
        {
            lock (_lock)
            {
                if (_clients.Contains(session))
                {
                    _clients.Remove(session);
                }

                // Nếu client đang ở trong hàng chờ thì phải loại bỏ ra khỏi hàng chờ
                var temporaryList = new List<ClientSession>(_joinQueue);
                if (temporaryList.Contains(session))
                {
                    temporaryList.Remove(session);
                    _joinQueue.Clear();
                    foreach (var s in temporaryList)
                    {
                        _joinQueue.Enqueue(s);
                    }
                    Console.WriteLine("[Hàng chờ] Đã loại bỏ client ngắt kết nối khỏi hàng chờ.");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("[Server] Đã dừng hoạt động.");
        }
    }
}