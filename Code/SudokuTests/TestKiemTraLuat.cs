using System;

namespace SudokuLogic
{
    /// <summary>Test kiểm tra hàng/cột/vùng 3x3. Gọi trong Main:  TestKiemTraLuat.Chay();</summary>
    public static class TestKiemTraLuat
    {
        private static int _dau = 0, _tong = 0;

        private static void Kiem(string ten, bool dieuKien)
        {
            _tong++;
            if (dieuKien) _dau++;
            Console.WriteLine($"[TEST {_tong}] {ten} -> " + (dieuKien ? "PASSED" : "FAILED"));
        }

        public static void Chay()
        {
            Console.WriteLine();
            Console.WriteLine("=== KIỂM THỬ HÀNG / CỘT / VÙNG 3x3 ===");
            _dau = 0; _tong = 0;

            var g = new SudokuGame();
            Kiem("Đề ban đầu không vi phạm luật", g.LaBanHopLe());

            g.DienSo(0, 2, 4); // đúng đáp án
            Kiem("Điền đúng -> không trùng", !g.CoTrungSo(0, 2));

            g = new SudokuGame();
            g.DienSo(0, 2, 5); // đề đã có số 5 ở (0,0): trùng hàng
            Kiem("Trùng số trong cùng hàng", g.CoTrungSo(0, 2));

            g = new SudokuGame();
            g.DienSo(8, 0, 5); // đề đã có số 5 ở (0,0): trùng cột
            Kiem("Trùng số trong cùng cột", g.CoTrungSo(8, 0));

            g = new SudokuGame();
            g.DienSo(1, 1, 8); // đề đã có số 8 ở (2,2): chỉ trùng vùng 3x3
            Kiem("Trùng số trong cùng vùng 3x3", g.CoTrungSo(1, 1));

            g = new SudokuGame();
            g.DienSo(4, 4, 5); // hàng 4, cột 4 và vùng giữa đều chưa có số 5
            Kiem("Số 5 ở giữa bàn không trùng", !g.CoTrungSo(4, 4));

            Kiem("Tọa độ ngoài bàn không gây lỗi", !g.CoTrungSo(9, 9) && !g.CoTrungSo(-1, 0));

            g = new SudokuGame();
            Kiem("Bàn chưa đầy không phải hoàn thành hợp lệ", !g.LaBanHoanThanhHopLe());

            g.GiaLapHoanThanhBanCo();
            Kiem("Bàn đầy đủ và đúng luật -> hoàn thành hợp lệ", g.LaBanHoanThanhHopLe());

            g.DienSo(0, 2, 5); // phá bàn đã hoàn thành
            Kiem("Bàn đầy nhưng có trùng số -> không hợp lệ", !g.LaBanHoanThanhHopLe());

            Console.WriteLine($"=> Kết quả: {_dau}/{_tong} test đạt.");
        }
    }
}
