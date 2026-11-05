using System;

namespace SudokuLogic
{
    /// <summary>
    /// Test console cho giai đoạn 2 (logic thắng/thua/hòa).
    /// Cách dùng: trong Main của Program.cs thêm một dòng:  TestGiaiDoan2.Chay();
    /// </summary>
    public static class TestGiaiDoan2
    {
        private static int _dau = 0;
        private static int _tong = 0;

        private static void Kiem(string ten, bool dieuKien)
        {
            _tong++;
            if (dieuKien) _dau++;
            Console.WriteLine($"[TEST {_tong}] {ten} -> " + (dieuKien ? "PASSED" : "FAILED"));
        }

        public static void Chay()
        {
            Console.WriteLine();
            Console.WriteLine("=== KIỂM THỬ LOGIC GIAI ĐOẠN 2 (THẮNG/THUA/HÒA) ===");
            _dau = 0;
            _tong = 0;

            // 1. Hoàn thành trước thì thắng
            var a = new SudokuGame();
            var b = new SudokuGame();
            a.GiaLapHoanThanhBanCo();
            var kq = TrongTai.KiemTraSauNuocDi(a, b, NguoiChoi.Nguoi1);
            Kiem("Người 1 hoàn thành trước -> thắng",
                kq != null && kq.NguoiThang == NguoiChoi.Nguoi1 && kq.LyDo == LyDoKetThuc.HoanThanhTruoc);

            // 2. Chưa hoàn thành thì trận tiếp tục (trả về null)
            a = new SudokuGame();
            b = new SudokuGame();
            a.DienSo(0, 2, 4);
            Kiem("Chưa hoàn thành -> trận tiếp tục", TrongTai.KiemTraSauNuocDi(a, b, NguoiChoi.Nguoi1) == null);

            // 3. Hết giờ: ai nhiều ô đúng hơn thì thắng
            a = new SudokuGame();
            b = new SudokuGame();
            a.DienSo(0, 2, 4); // đúng
            kq = TrongTai.XetKhiHetGio(a, b);
            Kiem("Hết giờ, người 1 nhiều ô đúng hơn -> người 1 thắng",
                kq.NguoiThang == NguoiChoi.Nguoi1 && kq.LyDo == LyDoKetThuc.HetGio && a.DiemSo == b.DiemSo + 1);

            // 4. Hết giờ: người 2 nhiều hơn thì người 2 thắng
            a = new SudokuGame();
            b = new SudokuGame();
            b.DienSo(0, 2, 4);
            b.DienSo(0, 3, 6);
            a.DienSo(0, 2, 4);
            kq = TrongTai.XetKhiHetGio(a, b);
            Kiem("Hết giờ, người 2 nhiều ô đúng hơn -> người 2 thắng", kq.NguoiThang == NguoiChoi.Nguoi2);

            // 5. Hết giờ: số ô đúng bằng nhau thì hòa
            a = new SudokuGame();
            b = new SudokuGame();
            a.DienSo(0, 2, 4);
            b.DienSo(0, 2, 4);
            kq = TrongTai.XetKhiHetGio(a, b);
            Kiem("Hết giờ, số ô đúng bằng nhau -> hòa", kq.LaHoa && kq.NguoiThang == NguoiChoi.Khong);

            // 6. Điền sai bị trừ 1 điểm
            a = new SudokuGame();
            b = new SudokuGame();
            int diemTruoc = a.DiemSo;
            a.DienSo(0, 2, 9); // sai (đáp án là 4)
            Kiem("Điền sai trừ 1 điểm", a.DiemSo == diemTruoc - 1);
            kq = TrongTai.XetKhiHetGio(a, b);
            Kiem("Hết giờ, người 1 điền sai nên điểm thấp hơn -> người 2 thắng", kq.NguoiThang == NguoiChoi.Nguoi2);

            // 6b. Điền sai rồi sửa đúng: được +1 ô đúng nhưng vẫn mất 1 điểm vì lần sai trước
            a = new SudokuGame();
            diemTruoc = a.DiemSo;
            a.DienSo(0, 2, 9);
            a.DienSo(0, 2, 4);
            Kiem("Điền sai rồi sửa đúng -> điểm không đổi (+1 -1)", a.DiemSo == diemTruoc);

            // 6c. Cùng số ô đúng nhưng một người điền sai nhiều hơn thì thua điểm
            a = new SudokuGame();
            b = new SudokuGame();
            a.DienSo(0, 2, 4);
            b.DienSo(0, 3, 9); // sai
            b.DienSo(0, 3, 6); // sửa đúng: cùng 1 ô đúng như người 1, nhưng đã bị trừ 1 điểm ở lần sai
            kq = TrongTai.XetKhiHetGio(a, b);
            Kiem("Cùng ô đúng nhưng người 2 từng điền sai -> người 1 thắng điểm", kq.NguoiThang == NguoiChoi.Nguoi1);

            // 7. Bỏ cuộc: người còn lại thắng (kể cả khi người bỏ cuộc đang dẫn điểm)
            a = new SudokuGame();
            b = new SudokuGame();
            a.DienSo(0, 2, 4);
            kq = TrongTai.XetKhiBoCuoc(a, b, NguoiChoi.Nguoi1);
            Kiem("Người 1 bỏ cuộc -> người 2 thắng",
                kq.NguoiThang == NguoiChoi.Nguoi2 && kq.NguoiThua == NguoiChoi.Nguoi1 && kq.LyDo == LyDoKetThuc.BoCuoc);

            // 8. Mất kết nối quá 60 giây mới bị xử thua
            kq = TrongTai.XetKhiHetThoiGianChoKetNoi(a, b, NguoiChoi.Nguoi2);
            Kiem("Người 2 mất kết nối quá thời gian chờ -> người 1 thắng",
                kq.NguoiThang == NguoiChoi.Nguoi1 && kq.LyDo == LyDoKetThuc.MatKetNoi);

            Kiem("Thời gian chờ kết nối lại là 60 giây", TrongTai.ThoiGianChoKetNoiGiay == 60);

            // 9. Dữ liệu không hợp lệ: không được đoán bừa, phải báo lỗi
            bool baoLoi = false;
            try { TrongTai.XetKhiBoCuoc(a, b, NguoiChoi.Khong); }
            catch (ArgumentException) { baoLoi = true; }
            Kiem("Người chơi không hợp lệ (Khong) -> báo lỗi", baoLoi);

            baoLoi = false;
            try { TrongTai.XetKhiHetGio(null!, b); }
            catch (ArgumentNullException) { baoLoi = true; }
            Kiem("Bàn chơi null -> báo lỗi", baoLoi);

            // 10. Hai bàn độc lập: điền ở bàn 1 không làm đổi bàn 2
            a = new SudokuGame();
            b = new SudokuGame();
            int diemB = b.DiemSo;
            a.DienSo(0, 2, 4);
            Kiem("Hai bàn chơi độc lập nhau", b.DiemSo == diemB && a.DiemSo == diemB + 1);

            // 11. Câu thông báo có nội dung
            kq = TrongTai.XetKhiHetGio(new SudokuGame(), new SudokuGame());
            Kiem("MoTa() trả về thông báo khi hòa", kq.MoTa().Contains("HÒA"));

            Console.WriteLine($"=> Kết quả: {_dau}/{_tong} test đạt.");
        }
    }
}
