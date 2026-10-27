using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace SudokuServer
{
    public class GameServer
    {
        private TcpListener _listener;
        private List<ClientSession> _clients = new List<ClientSession>();
        private ConcurrentQueue<ClientSession> _waitingQueue = new ConcurrentQueue<ClientSession>();
        private bool _isRunning = false;

        public GameServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[Server đang chạy] Đang lắng nghe cổng {_listener.LocalEndpoint}...");

            // Bắt đầu chấp nhận kết nối bất đồng bộ
            _listener.BeginAcceptTcpClient(OnClientConnected, null);
        }

        private void OnClientConnected(IAsyncResult ar)
        {
            try
            {
                TcpClient tcpClient = _listener.EndAcceptTcpClient(ar);
                Console.WriteLine($"[Kết nối mới] Có Client vừa kết nối từ: {tcpClient.Client.RemoteEndPoint}");

                var session = new ClientSession(tcpClient, this);
                lock (_clients)
                {
                    _clients.Add(session);
                }

                session.StartReceiving();

                // Tiếp tục lắng nghe các kết nối tiếp theo
                if (_isRunning)
                {
                    _listener.BeginAcceptTcpClient(OnClientConnected, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi chấp nhận kết nối]: {ex.Message}");
            }
        }

        public void HandleClientRequest(ClientSession session, RequestMessage request)
        {
            Console.WriteLine($"[Nhận lệnh từ Client] Type: {request.Type} | Payload: {request.Payload}");

            if (request.Type == "JOIN_QUEUE")
            {
                // Đưa Client vào hàng chờ ghép trận
                _waitingQueue.Enqueue(session);
                
                session.SendMessage(new ResponseMessage
                {
                    Status = "WAITING",
                    Message = "Đã vào hàng chờ. Đang tìm đối thủ..."
                });

                CheckMatchmaking();
            }
            // Sau này bạn có thể bổ sung các lệnh khác như SUBMIT_MOVE ở đây
        }

        private void CheckMatchmaking()
        {
            // Kiểm tra xem hàng chờ có đủ từ 2 người chơi trở lên hay không
            lock (_waitingQueue)
            {
                if (_waitingQueue.Count >= 2)
                {
                    if (_waitingQueue.TryDequeue(out var player1) && _waitingQueue.TryDequeue(out var player2))
                    {
                        Console.WriteLine("[Ghép trận thành công] Đã tìm thấy 2 người chơi cho trận đấu mới!");

                        var matchNotification = new ResponseMessage
                        {
                            Status = "MATCH_FOUND",
                            Message = "Đã tìm thấy đối thủ! Trận đấu bắt đầu."
                        };

                        player1.SendMessage(matchNotification);
                        player2.SendMessage(matchNotification);

                        // TODO: Khởi tạo đối tượng Match cho 2 player này tại đây
                    }
                }
            }
        }

        public void RemoveClient(ClientSession session)
        {
            lock (_clients)
            {
                _clients.Remove(session);
            }
        }
    }
}