using System;

namespace SudokuLogic
{
    /// <summary>Người chơi trong một trận. "Khong" dùng cho kết quả hòa.</summary>
    public enum NguoiChoi
    {
        Khong = 0,   // Không ai (hòa)
        Nguoi1 = 1,
        Nguoi2 = 2
    }

    /// <summary>Lý do trận đấu kết thúc.</summary>
    public enum LyDoKetThuc
    {
        HoanThanhTruoc, // Có người hoàn thành bảng hợp lệ trước
        HetGio,         // Hết 10 phút, so sánh số ô đúng
        BoCuoc,         // Một người chủ động bỏ cuộc
        MatKetNoi       // Một người mất kết nối và không quay lại kịp thời gian chờ
    }

    /// <summary>Kết quả cuối cùng của một trận.</summary>
    public class KetQuaTran
    {
        /// <summary>Người thắng. Bằng NguoiChoi.Khong nghĩa là hòa.</summary>
        public NguoiChoi NguoiThang { get; }

        /// <summary>Người thua. Bằng NguoiChoi.Khong nghĩa là hòa.</summary>
        public NguoiChoi NguoiThua { get; }

        public LyDoKetThuc LyDo { get; }

        /// <summary>Điểm của người 1 tại thời điểm kết thúc (điền đúng +1, điền sai -1).</summary>
        public int Diem1 { get; }

        /// <summary>Điểm của người 2 tại thời điểm kết thúc (điền đúng +1, điền sai -1).</summary>
        public int Diem2 { get; }

        public bool LaHoa => NguoiThang == NguoiChoi.Khong;

        public KetQuaTran(NguoiChoi nguoiThang, LyDoKetThuc lyDo, int diem1, int diem2)
        {
            NguoiThang = nguoiThang;
            NguoiThua = nguoiThang == NguoiChoi.Nguoi1 ? NguoiChoi.Nguoi2
                      : nguoiThang == NguoiChoi.Nguoi2 ? NguoiChoi.Nguoi1
                      : NguoiChoi.Khong;
            LyDo = lyDo;
            Diem1 = diem1;
            Diem2 = diem2;
        }

        /// <summary>Câu thông báo tiếng Việt, dùng để ghi log hoặc hiện lên Client.</summary>
        public string MoTa()
        {
            string nguoiThang = NguoiThang == NguoiChoi.Nguoi1 ? "Người chơi 1" : "Người chơi 2";
            string nguoiThua = NguoiThua == NguoiChoi.Nguoi1 ? "Người chơi 1" : "Người chơi 2";

            switch (LyDo)
            {
                case LyDoKetThuc.HoanThanhTruoc:
                    return nguoiThang + " hoàn thành bảng hợp lệ trước và thắng.";
                case LyDoKetThuc.HetGio:
                    return LaHoa
                        ? $"Hết giờ, hai bên cùng {Diem1} điểm: HÒA."
                        : $"Hết giờ, {nguoiThang} có điểm cao hơn ({Diem1} - {Diem2}) và thắng.";
                case LyDoKetThuc.BoCuoc:
                    return nguoiThua + " bỏ cuộc, " + nguoiThang + " thắng.";
                case LyDoKetThuc.MatKetNoi:
                    return nguoiThua + " mất kết nối quá thời gian chờ, " + nguoiThang + " thắng.";
                default:
                    return "Trận đấu đã kết thúc.";
            }
        }
    }
}
