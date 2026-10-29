using System;
using System.Collections.Generic;

namespace SudokuLogic
{
    public class SudokuGame
    {
        private static readonly (int[,] De, int[,] DapAn) DeMau = (
            new int[9, 9] {
                {5,3,0, 0,7,0, 0,0,0}, {6,0,0, 1,9,5, 0,0,0}, {0,9,8, 0,0,0, 0,6,0},
                {8,0,0, 0,6,0, 0,0,3}, {4,0,0, 8,0,3, 0,0,1}, {7,0,0, 0,2,0, 0,0,6},
                {0,6,0, 0,0,0, 2,8,0}, {0,0,0, 4,1,9, 0,0,5}, {0,0,0, 0,8,0, 0,7,9}
            },
            new int[9, 9] {
                {5,3,4, 6,7,8, 9,1,2}, {6,7,2, 1,9,5, 3,4,8}, {1,9,8, 3,4,2, 5,6,7},
                {8,5,9, 7,6,1, 4,2,3}, {4,2,6, 8,5,3, 7,9,1}, {7,1,3, 9,2,4, 8,5,6},
                {9,6,1, 5,3,7, 2,8,4}, {2,8,7, 4,1,9, 6,3,5}, {3,4,5, 2,8,6, 1,7,9}
            }
        );

        public int[,] DeBai { get; } = (int[,])DeMau.De.Clone();
        public int[,] DapAn { get; } = (int[,])DeMau.DapAn.Clone();
        public int[,] BanCoHienTai { get; } = (int[,])DeMau.De.Clone();

        public bool LaOCoDinh(int r, int c) => DeBai[r, c] != 0;

        public bool DienSo(int r, int c, int val) {
            if (LaOCoDinh(r, c)) return false;
            BanCoHienTai[r, c] = val;
            return true;
        }

        public int DemSoODung() {
            int diem = 0;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (BanCoHienTai[r, c] != 0 && BanCoHienTai[r, c] == DapAn[r, c]) diem++;
            return diem;
        }

        public bool KiemTraHoanThanh() => DemSoODung() == 81;

        public void GiaLapHoanThanhBanCo() {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++) BanCoHienTai[r, c] = DapAn[r, c];
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var game = new SudokuGame();

            Console.WriteLine("=== KIỂM THỬ LOGIC GIAI ĐOẠN 1 ===");
            
            // Test 1: Khởi tạo
            int diemBD = game.DemSoODung();
            Console.WriteLine($"[TEST 1] Điểm ban đầu: {diemBD}/81 -> " + (diemBD > 0 ? "PASSED" : "FAILED"));

            // Test 2: Ô cố định
            Console.WriteLine("[TEST 2] Chặn sửa ô đề bài -> " + (!game.DienSo(0, 0, 9) ? "PASSED" : "FAILED"));

            // Test 3: Điền đúng
            game.DienSo(0, 2, 4);
            Console.WriteLine("[TEST 3] Điền đúng +1 điểm -> " + (game.DemSoODung() == diemBD + 1 ? "PASSED" : "FAILED"));

            // Test 4: Điền sai
            game.DienSo(0, 3, 9);
            Console.WriteLine("[TEST 4] Điền sai không tăng điểm -> " + (game.DemSoODung() == diemBD + 1 ? "PASSED" : "FAILED"));

            // Test 5: Hoàn thành
            game.GiaLapHoanThanhBanCo();
            Console.WriteLine("[TEST 5] Kiểm tra thắng (81/81) -> " + (game.KiemTraHoanThanh() ? "PASSED" : "FAILED"));
        }
    }
}