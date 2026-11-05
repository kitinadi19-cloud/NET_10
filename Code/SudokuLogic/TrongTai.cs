using System;

namespace SudokuLogic
{
    /// <summary>
    /// "Trọng tài" của trận đấu: nhận hai bàn chơi (mỗi người một SudokuGame)
    /// rồi quyết định ai thắng, ai thua, hay hòa theo luật nhóm.
    /// Server (TV2) chỉ cần gọi các hàm ở đây, không cần tự so sánh điểm.
    ///
    /// Luật:
    ///  - Điểm (SudokuGame.DiemSo): điền đúng +1, điền sai -1.
    ///  - Người hoàn thành bảng hợp lệ trước thì thắng.
    ///  - Hết giờ mà chưa ai hoàn thành: điểm cao hơn thắng, bằng nhau thì hòa.
    ///  - Một người bỏ cuộc: người còn lại thắng.
    ///  - Một người mất kết nối: được chờ 60 giây để kết nối lại; quá 60 giây thì xử thua.
    /// </summary>
    public static class TrongTai
    {
        /// <summary>Số giây được chờ người mất kết nối quay lại (Server dùng để đặt đồng hồ chờ).</summary>
        public const int ThoiGianChoKetNoiGiay = 60;

        /// <summary>
        /// Gọi sau MỖI nước đi hợp lệ của một người chơi.
        /// Trả về kết quả nếu người vừa đi đã hoàn thành bảng; ngược lại trả về null (trận tiếp tục).
        /// </summary>
        public static KetQuaTran? KiemTraSauNuocDi(SudokuGame ban1, SudokuGame ban2, NguoiChoi nguoiVuaDi)
        {
            KiemTraNguoiChoiHopLe(nguoiVuaDi);
            KiemTraBan(ban1, ban2);

            SudokuGame banNguoiDi = nguoiVuaDi == NguoiChoi.Nguoi1 ? ban1 : ban2;
            if (!banNguoiDi.KiemTraHoanThanh())
                return null;

            return new KetQuaTran(nguoiVuaDi, LyDoKetThuc.HoanThanhTruoc,
                ban1.DiemSo, ban2.DiemSo);
        }

        /// <summary>Gọi khi đồng hồ về 0 mà trận chưa kết thúc. So sánh điểm.</summary>
        public static KetQuaTran XetKhiHetGio(SudokuGame ban1, SudokuGame ban2)
        {
            KiemTraBan(ban1, ban2);

            int diem1 = ban1.DiemSo;
            int diem2 = ban2.DiemSo;

            NguoiChoi nguoiThang = diem1 > diem2 ? NguoiChoi.Nguoi1
                                 : diem2 > diem1 ? NguoiChoi.Nguoi2
                                 : NguoiChoi.Khong; // bằng nhau thì hòa

            return new KetQuaTran(nguoiThang, LyDoKetThuc.HetGio, diem1, diem2);
        }

        /// <summary>Gọi khi một người chủ động bỏ cuộc. Người còn lại thắng.</summary>
        public static KetQuaTran XetKhiBoCuoc(SudokuGame ban1, SudokuGame ban2, NguoiChoi nguoiBoCuoc)
        {
            return XetKhiNguoiRoiTran(ban1, ban2, nguoiBoCuoc, LyDoKetThuc.BoCuoc);
        }

        /// <summary>
        /// Gọi khi một người đã mất kết nối quá ThoiGianChoKetNoiGiay (60 giây) mà chưa quay lại.
        /// Nếu người đó kết nối lại kịp thì Server KHÔNG gọi hàm này, trận tiếp tục bình thường.
        /// </summary>
        public static KetQuaTran XetKhiHetThoiGianChoKetNoi(SudokuGame ban1, SudokuGame ban2, NguoiChoi nguoiMatKetNoi)
        {
            return XetKhiNguoiRoiTran(ban1, ban2, nguoiMatKetNoi, LyDoKetThuc.MatKetNoi);
        }

        // Dùng chung cho bỏ cuộc và mất kết nối quá hạn: người rời trận thua, người kia thắng.
        private static KetQuaTran XetKhiNguoiRoiTran(SudokuGame ban1, SudokuGame ban2,
            NguoiChoi nguoiRoi, LyDoKetThuc lyDo)
        {
            KiemTraNguoiChoiHopLe(nguoiRoi);
            KiemTraBan(ban1, ban2);

            NguoiChoi nguoiThang = nguoiRoi == NguoiChoi.Nguoi1 ? NguoiChoi.Nguoi2 : NguoiChoi.Nguoi1;
            return new KetQuaTran(nguoiThang, lyDo, ban1.DiemSo, ban2.DiemSo);
        }

        // Chỉ chấp nhận Nguoi1 hoặc Nguoi2 (không chấp nhận Khong hay giá trị lạ).
        private static void KiemTraNguoiChoiHopLe(NguoiChoi nguoi)
        {
            if (nguoi != NguoiChoi.Nguoi1 && nguoi != NguoiChoi.Nguoi2)
                throw new ArgumentException("Người chơi phải là Nguoi1 hoặc Nguoi2.", nameof(nguoi));
        }

        private static void KiemTraBan(SudokuGame ban1, SudokuGame ban2)
        {
            if (ban1 == null) throw new ArgumentNullException(nameof(ban1));
            if (ban2 == null) throw new ArgumentNullException(nameof(ban2));
        }
    }
}
