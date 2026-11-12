# UDM_17 – Game Sudoku Đối Kháng Qua Server

Client–Server qua TCP (JSON, `\n`), C# / .NET 10, GUI Windows Forms (không phải Web app).

## Cấu trúc
| Thư mục | Nội dung |
|---|---|
| `SudokuShared` | Message, DTO, hằng số giao thức, `JsonHelper` |
| `SudokuLogic` | Sinh đề (nghiệm duy nhất), kiểm tra nước đi, tính điểm (`PlayerBoard`) |
| `SudokuServer` | Console server: ghép trận, thách đấu, trận đấu, đồng hồ, khán giả, ngắt/kết nối lại, log, lịch sử |
| `SudokuClient` | GUI WinForms: sảnh, màn chơi, màn khán giả, tự kết nối lại |
| `SudokuTests` | xUnit: logic, luật trận, test mạng (có test lỗi/ngắt kết nối và stress 20 trận) |

## Chạy
```bash
dotnet build SudokuUDM17.slnx
dotnet run --project SudokuServer            # cổng 8888, hoặc: dotnet run --project SudokuServer -- 9000
dotnet run --project SudokuClient            # mở 2 client để chơi, client thứ 3 để xem
dotnet test                                  # chạy toàn bộ kiểm thử
```
Chạy trên nhiều máy: nhập IP máy server vào ô "Server IP" của Client (mở firewall cổng 8888).

## Video demo
Link: _(dán link YouTube Public/Unlisted vào đây)_

## Giới hạn
- Chưa có đăng nhập mật khẩu (tên chỉ cần duy nhất).
- Lịch sử lưu file `.jsonl`, chưa có màn hình xem lịch sử.
- Chưa có báo cáo stress/performance bằng số liệu thật: hãy chạy `dotnet test`, ghi cấu hình máy + kết quả + ảnh/log vào báo cáo.
