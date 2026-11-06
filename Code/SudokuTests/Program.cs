using System;
namespace SudokuLogic
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("==============================================");
            Console.WriteLine("=== LOGIC SUDOKU (TV3) ===");
            Console.WriteLine("==============================================");

            TestDienSo.Chay();        // điểm, dữ liệu không hợp lệ
            TestKiemTraLuat.Chay();   // hàng / cột / vùng 3x3
            TestGiaiDoan2.Chay();     // thắng / thua / hòa
        }
    }
}