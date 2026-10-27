using System;

namespace SudokuServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int port = 8888; // Cổng kết nối mặc định

            GameServer server = new GameServer(port);
            server.Start();

            Console.WriteLine("Nhấn phím 'Enter' để dừng Server...");
            Console.ReadLine();
        }
    }
}