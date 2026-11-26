# Game Sudoku Đối Kháng Qua Server

- **Project Code:** UDM_17
- **Trạng thái:** Đang phân tích và phát triển.

## 1. Giới thiệu
Xây dựng trò chơi Sudoku đối kháng giữa hai người chơi thông qua mô hình Client–Server. Server quản lý trận đấu, kiểm tra nước đi và xác định kết quả.

## 2. Công nghệ dự kiến
- Ngôn ngữ: C#
- Nền tảng: .NET
- Giao diện: Windows Forms (WinForms)
- Giao tiếp mạng: TCP Socket bất đồng bộ
- Định dạng dữ liệu: JSON
- Quản lý mã nguồn: Git và GitHub

## 3. Chức năng chính
- Hai người chơi cùng giải một đề Sudoku, có tiến độ riêng.
- Server kiểm tra nước đi và quản lý kết quả trận đấu.
- Hỗ trợ nhiều trận đấu và lưu lịch sử.
- Khán giả có thể theo dõi trận đấu theo thời gian thực.

## 4. Thành viên nhóm

| STT | Họ và tên | Vai trò |
|---|---|---|
| 1 | Nguyễn Chánh Tư Duy | Nhóm trưởng, quản lý dự án |
| 2 | Nguyễn Thành Đạt | Server và giao tiếp mạng |
| 3 | Phan Trần Quang Lâm | Logic Sudoku |
| 4 | Nguyễn Chí Hiến | Lưu trữ dữ liệu, lịch sử và khán giả |
| 5 | Nguyễn Anh Khôi | Giao diện Client|
| 6 | Hoàng Thành Đạt | Kiểm thử và tài liệu |

*Vai trò dự kiến, có thể điều chỉnh theo phân công thực tế của nhóm.*

## 5. Cấu trúc
``` text
NET_10/
├── Code/
│   ├── SudokuClient/  --Message, DTO, hằng số giao thức, `JsonHelper`
│   ├── SudokuLogic/   --Sinh đề (nghiệm duy nhất), kiểm tra nước đi, tính điểm (`PlayerBoard`)
│   ├── SudokuServer/  --Console server: ghép trận, thách đấu, trận đấu, đồng hồ, khán giả, ngắt/kết nối lại, log, lịch sử
│   ├── SudokuShared/  --GUI WinForms: sảnh, màn chơi, màn khán giả, tự kết nối lại
│   ├── SudokuTests/   --xUnit: logic, luật trận, test mạng (có test lỗi/ngắt kết nối và stress 20 trận)
│   └── SudokuUDM17.slnx
├── Docx/   --file báo cáo
├── Extra/  --một số luật
├── PPTX/   --chứa file thuyết trình
├── .gitignore
└── README.md  --Mô tả đề tài nhóm và hướng dẫn demo
```

## 6. Chạy
```bash
dotnet build SudokuUDM17.slnx
dotnet run --project SudokuServer            # cổng 8888, hoặc: dotnet run --project SudokuServer -- 9000
dotnet run --project SudokuClient            # mở 2 client để chơi, client thứ 3 để xem
dotnet test                                  # chạy toàn bộ kiểm thử
```
Chạy trên nhiều máy: nhập IP máy server vào ô "Server IP" của Client (mở firewall cổng 8888).

