using System;

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

        public int DiemSo { get; private set; } = 0;

        public bool LaOCoDinh(int r, int c) => DeBai[r, c] != 0;

        /// <summary>
        /// Hàm điền số đã fix bug lặp điểm và kiểm tra tọa độ hợp lệ
        /// </summary>
        public bool DienSo(int r, int c, int val)
        {
            // 1. Kiểm tra tọa độ hợp lệ (0..8) và giá trị hợp lệ (0..9) [Fix Bug 2]
            if (r < 0 || r > 8 || c < 0 || c > 8 || val < 0 || val > 9) return false;

            // 2. Không cho sửa ô đề bài cố định
            if (LaOCoDinh(r, c)) return false;

            int giatriCu = BanCoHienTai[r, c];
            if (giatriCu == val) return true; // Điền lại đúng số cũ -> Không thay đổi điểm [Fix Bug 1]

            // Trừ điểm cũ nếu trước đó ô này đã điền đúng
            if (giatriCu != 0 && giatriCu == DapAn[r, c])
            {
                DiemSo -= 1;
            }

            // Cập nhật giá trị mới
            BanCoHienTai[r, c] = val;

            // Cộng/Trừ điểm theo giá trị mới
            if (val != 0)
            {
                if (val == DapAn[r, c]) DiemSo += 1; // Đúng -> +1đ
                else DiemSo -= 1;                    // Sai  -> -1đ
            }

            return true;
        }

        public int DemSoODung()
        {
            int diem = 0;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (BanCoHienTai[r, c] != 0 && BanCoHienTai[r, c] == DapAn[r, c]) diem++;
            return diem;
        }

        public bool KiemTraHoanThanh()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (BanCoHienTai[r, c] != DapAn[r, c]) return false;
            return true;
        }

        public void GiaLapHoanThanhBanCo()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++) BanCoHienTai[r, c] = DapAn[r, c];
        }
    }
}