namespace SudokuLogic
{
    /// <summary>
    /// Kiểm tra luật Sudoku: không trùng số trong hàng, cột và vùng 3x3.
    /// Viết dưới dạng "hàm mở rộng" nên dùng được ngay trên SudokuGame:  game.CoTrungSo(0, 2)
    /// (không cần sửa file SudokuGame hiện có).
    /// </summary>
    public static class SudokuKiemTraLuat
    {
        /// <summary>
        /// Số ở ô (r, c) có bị trùng trong hàng, cột hoặc vùng 3x3 không?
        /// Ô trống hoặc tọa độ ngoài 0..8 thì trả về false.
        /// </summary>
        public static bool CoTrungSo(this SudokuGame game, int r, int c)
        {
            if (r < 0 || r > 8 || c < 0 || c > 8) return false;

            int so = game.BanCoHienTai[r, c];
            if (so == 0) return false;

            // Cùng hàng hoặc cùng cột
            for (int i = 0; i < 9; i++)
            {
                if (i != c && game.BanCoHienTai[r, i] == so) return true;
                if (i != r && game.BanCoHienTai[i, c] == so) return true;
            }

            // Cùng vùng 3x3
            int hangDau = r / 3 * 3;
            int cotDau = c / 3 * 3;
            for (int hang = hangDau; hang < hangDau + 3; hang++)
                for (int cot = cotDau; cot < cotDau + 3; cot++)
                    if ((hang != r || cot != c) && game.BanCoHienTai[hang, cot] == so)
                        return true;

            return false;
        }

        /// <summary>Bàn hiện tại có ô nào vi phạm luật (trùng hàng/cột/vùng) không?</summary>
        public static bool LaBanHopLe(this SudokuGame game)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (game.CoTrungSo(r, c)) return false;
            return true;
        }

        /// <summary>Bàn đã điền đủ 81 ô và không vi phạm luật nào.</summary>
        public static bool LaBanHoanThanhHopLe(this SudokuGame game)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (game.BanCoHienTai[r, c] == 0) return false;
            return game.LaBanHopLe();
        }
    }
}
