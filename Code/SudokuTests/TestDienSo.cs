using System;

namespace SudokuLogic
{
    /// <summary>Test hàm DienSo: tính điểm và xử lý dữ liệu không hợp lệ.</summary>
    public static class TestDienSo
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
            Console.WriteLine("=== KIỂM THỬ ĐIỀN SỐ VÀ TÍNH ĐIỂM ===");
            _dau = 0; _tong = 0;

            var g = new SudokuGame();
            Kiem("Điểm ban đầu bằng 0", g.DiemSo == 0);

            g.DienSo(0, 2, 9); // sai (đáp án 4)
            Kiem("Điền sai -> trừ 1 điểm", g.DiemSo == -1);

            g.DienSo(0, 2, 4); // sửa đúng
            Kiem("Sửa lại đúng -> cộng 1 điểm", g.DiemSo == 0);

            g.DienSo(0, 2, 4); // điền lại đúng số cũ nhiều lần
            g.DienSo(0, 2, 4);
            Kiem("Điền lặp lại số đúng -> không cộng điểm thêm", g.DiemSo == 0);

            g = new SudokuGame();
            g.DienSo(0, 2, 4);
            g.DienSo(0, 2, 9); // đang đúng, đổi sang sai: mất 1 điểm ô đúng và bị trừ 1 điểm điền sai
            Kiem("Đổi ô đúng thành sai -> mất điểm ô đó và bị trừ 1", g.DiemSo == -1);

            g = new SudokuGame();
            g.DienSo(0, 2, 4);
            g.DienSo(0, 2, 0); // xóa ô đang đúng
            Kiem("Xóa ô đang đúng -> mất điểm ô đó, không bị phạt thêm", g.DiemSo == 0);

            g = new SudokuGame();
            Kiem("Ô đề cố định không sửa được", !g.DienSo(0, 0, 9) && g.BanCoHienTai[0, 0] == 5 && g.DiemSo == 0);
            Kiem("Tọa độ ngoài bàn bị từ chối", !g.DienSo(9, 0, 1) && !g.DienSo(0, -1, 1) && g.DiemSo == 0);
            Kiem("Giá trị ngoài 0..9 bị từ chối", !g.DienSo(0, 2, 10) && !g.DienSo(0, 2, -1) && g.DiemSo == 0);

            g = new SudokuGame();
            Kiem("Bàn chưa điền thì chưa hoàn thành", !g.KiemTraHoanThanh());
            g.GiaLapHoanThanhBanCo();
            Kiem("Bàn điền đủ đáp án -> hoàn thành", g.KiemTraHoanThanh());

            Console.WriteLine($"=> Kết quả: {_dau}/{_tong} test đạt.");
        }
    }
}
