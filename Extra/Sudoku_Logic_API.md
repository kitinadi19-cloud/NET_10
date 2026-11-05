# Tài liệu API module Sudoku.Logic (TV3)

Namespace: `SudokuLogic`. Module không phụ thuộc WinForms hay mạng.
Mỗi người chơi có **một** đối tượng `SudokuGame` riêng; hai đối tượng cùng đề.

## SudokuGame (bàn chơi của một người)
| Hàm / thuộc tính | Ý nghĩa |
|---|---|
| `DeBai` | Đề bài 9x9 (0 = ô trống) |
| `DapAn` | Đáp án chuẩn 9x9 |
| `BanCoHienTai` | Trạng thái bàn của người chơi |
| `LaOCoDinh(r, c)` | `true` nếu là ô đề bài, không được sửa |
| `DienSo(r, c, val)` | Điền số (val = 0 là xóa ô). Trả `false` và không đổi gì nếu tọa độ ngoài 0..8, giá trị ngoài 0..9 hoặc ô cố định |
| `DiemSo` | Điểm: điền đúng +1, điền sai -1 (điền lại đúng số cũ không đổi điểm) |
| `KiemTraHoanThanh()` | `true` khi đủ 81/81 ô đúng |

## TrongTai (quyết định kết quả trận)
Hằng số luật: `ThoiGianChoKetNoiGiay` = 60.

| Hàm | Khi nào Server gọi | Trả về |
|---|---|---|
| `KiemTraSauNuocDi(ban1, ban2, nguoiVuaDi)` | Sau mỗi nước đi hợp lệ | `KetQuaTran` nếu người vừa đi hoàn thành, ngược lại `null` |
| `XetKhiHetGio(ban1, ban2)` | Đồng hồ về 0 | `KetQuaTran` (điểm cao hơn thắng, bằng nhau hòa) |
| `XetKhiBoCuoc(ban1, ban2, nguoiBoCuoc)` | Có người bỏ cuộc | Người còn lại thắng |
| `XetKhiHetThoiGianChoKetNoi(ban1, ban2, nguoiMat)` | Người mất kết nối đã quá 60 giây mà chưa quay lại | Người đó thua. Nếu họ kết nối lại kịp thì KHÔNG gọi hàm này |

`nguoiVuaDi`, `nguoiBoCuoc`, `nguoiMat` chỉ nhận `NguoiChoi.Nguoi1` hoặc `NguoiChoi.Nguoi2`;
giá trị khác sẽ ném `ArgumentException`.

## KetQuaTran
`NguoiThang`, `NguoiThua` (`NguoiChoi.Khong` = hòa), `LyDo` (`HoanThanhTruoc`, `HetGio`, `BoCuoc`, `MatKetNoi`),
`Diem1`, `Diem2`, `LaHoa`, `MoTa()` (câu thông báo tiếng Việt cho log/Client).

## Ví dụ cho Server
```csharp
var ket = TrongTai.KiemTraSauNuocDi(ban1, ban2, NguoiChoi.Nguoi1);
if (ket != null) { /* kết thúc trận, gửi ket.MoTa() cho hai Client, lưu lịch sử */ }
```

## Kiểm tra luật hàng / cột / vùng 3x3 (SudokuKiemTraLuat)
| Hàm | Ý nghĩa |
|---|---|
| `game.CoTrungSo(r, c)` | Số ở ô (r, c) có trùng trong hàng, cột hoặc vùng 3x3 không |
| `game.LaBanHopLe()` | Bàn hiện tại không có ô nào trùng số |
| `game.LaBanHoanThanhHopLe()` | Bàn đủ 81 ô và không trùng số nào |
